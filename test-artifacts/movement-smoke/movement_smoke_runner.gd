extends SceneTree

const ROOT_DIR := "E:/GitHub Projects/Restaurant Simulator/Restaurant Simulator/test-artifacts/movement-smoke"

var scene: Node
var smoke_camera: Camera3D
var out_dir: String
var office_roundtrip := {
	"status": "not_run",
	"agent_id": "",
	"departed": false,
	"returned": false,
	"depart_frame": -1,
	"return_frame": -1,
	"max_distance_m": 0.0
}

func _initialize() -> void:
	root.size = Vector2i(1600, 900)
	var stamp := Time.get_datetime_string_from_system(false, true).replace(":", "").replace("-", "").replace(" ", "_")
	out_dir = ROOT_DIR.path_join(stamp)
	DirAccess.make_dir_recursive_absolute(out_dir)
	OS.set_environment("RS_MOVEMENT_TELEMETRY_DIR", out_dir)
	var packed := load("res://scenes/Main.tscn")
	scene = packed.instantiate()
	root.add_child(scene)
	call_deferred("_run")

func _run() -> void:
	await _frames(120)
	_hide_node_named(scene, "RoofGroup")
	_install_overhead_camera()
	_key(KEY_SPACE)
	_key(KEY_R)
	await _frames(120)
	_capture("00_start.png")
	for i in range(1, 7):
		if i == 1:
			await _office_roundtrip_probe(1800)
		else:
			await _frames(900)
		_capture("%02d_overhead.png" % i)
	_write_office_roundtrip()
	print("[MovementSmoke] output_dir=", out_dir)
	quit()

func _install_overhead_camera() -> void:
	smoke_camera = Camera3D.new()
	smoke_camera.name = "MovementSmokeOverheadCamera"
	smoke_camera.position = Vector3(3.5, 31.0, -1.5)
	smoke_camera.rotation_degrees = Vector3(-90, 0, 0)
	smoke_camera.fov = 62.0
	scene.add_child(smoke_camera)
	smoke_camera.current = true

func _frames(count: int) -> void:
	for i in count:
		await process_frame

func _key(code: Key) -> void:
	var ev := InputEventKey.new()
	ev.keycode = code
	ev.physical_keycode = code
	ev.pressed = true
	root.push_input(ev)
	ev = InputEventKey.new()
	ev.keycode = code
	ev.physical_keycode = code
	ev.pressed = false
	root.push_input(ev)

func _capture(name: String) -> void:
	await process_frame
	if DisplayServer.get_name() == "headless":
		print("[MovementSmoke] skipped ", name, " in headless display mode")
		return
	var tex := root.get_texture()
	if tex == null:
		print("[MovementSmoke] skipped ", name, " because viewport texture is null")
		return
	var img := tex.get_image()
	if img == null:
		print("[MovementSmoke] skipped ", name, " because viewport image is null")
		return
	img.save_png(out_dir.path_join(name))
	print("[MovementSmoke] wrote ", out_dir.path_join(name), " size=", img.get_width(), "x", img.get_height())

func _office_roundtrip_probe(frame_budget: int) -> void:
	var employee := _office_employee()
	if employee == null:
		office_roundtrip.status = "no_office_employee"
		await _frames(frame_budget)
		return

	office_roundtrip.agent_id = str(employee.get("AgentId"))
	var home := (employee as Node3D).global_position
	var greet_target := Node3D.new()
	greet_target.name = "OfficeRoundtripTarget"
	scene.add_child(greet_target)
	greet_target.position = Vector3(-2.2, 0.0, -9.4)
	employee.set("Patrols", true)
	employee.call("GoGreet", greet_target)
	var remaining := frame_budget
	var depart_frame := -1
	for frame in range(frame_budget):
		await process_frame
		remaining -= 1
		if not is_instance_valid(employee):
			office_roundtrip.status = "employee_freed"
			greet_target.queue_free()
			await _frames(remaining)
			return
		var distance := _flat_distance((employee as Node3D).global_position, home)
		office_roundtrip.max_distance_m = max(office_roundtrip.max_distance_m, distance)
		if distance > 2.0:
			depart_frame = frame
			office_roundtrip.departed = true
			office_roundtrip.depart_frame = frame
			employee.call("ReturnToStation")
			break

	if depart_frame < 0:
		office_roundtrip.status = "no_departure"
		greet_target.queue_free()
		await _frames(remaining)
		return

	greet_target.queue_free()
	for frame in range(remaining):
		await process_frame
		if not is_instance_valid(employee):
			office_roundtrip.status = "employee_freed_after_departure"
			return
		var distance := _flat_distance((employee as Node3D).global_position, home)
		office_roundtrip.max_distance_m = max(office_roundtrip.max_distance_m, distance)
		if distance <= 0.75:
			office_roundtrip.returned = true
			office_roundtrip.return_frame = depart_frame + frame
			office_roundtrip.status = "pass"
			return

	office_roundtrip.status = "no_return"

func _office_employee() -> Node:
	var agents := scene.get_node_or_null("Agents")
	if agents == null:
		return null
	for child in agents.get_children():
		if child.get("StationKey") == "work_office":
			return child
	return null

func _flat_distance(a: Vector3, b: Vector3) -> float:
	var dx := a.x - b.x
	var dz := a.z - b.z
	return sqrt(dx * dx + dz * dz)

func _write_office_roundtrip() -> void:
	var f := FileAccess.open(out_dir.path_join("manager_office_roundtrip.json"), FileAccess.WRITE)
	if f == null:
		print("[MovementSmoke] failed to write manager_office_roundtrip.json")
		return
	f.store_string(JSON.stringify(office_roundtrip, "\t"))
	f.close()
	print("[MovementSmoke] office_roundtrip=", JSON.stringify(office_roundtrip))

func _hide_node_named(n: Node, target_name: String) -> bool:
	if n.name == target_name:
		if n is Node3D:
			n.visible = false
		return true
	for child in n.get_children():
		if _hide_node_named(child, target_name):
			return true
	return false
