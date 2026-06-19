extends SceneTree

const ROOT_DIR := "E:/GitHub Projects/Restaurant Simulator/Restaurant Simulator/test-artifacts/movement-smoke"

var scene: Node
var smoke_camera: Camera3D
var out_dir: String

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
		await _frames(900)
		_capture("%02d_overhead.png" % i)
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
	var img := root.get_texture().get_image()
	img.save_png(out_dir.path_join(name))
	print("[MovementSmoke] wrote ", out_dir.path_join(name), " size=", img.get_width(), "x", img.get_height())

func _hide_node_named(n: Node, target_name: String) -> bool:
	if n.name == target_name:
		if n is Node3D:
			n.visible = false
		return true
	for child in n.get_children():
		if _hide_node_named(child, target_name):
			return true
	return false
