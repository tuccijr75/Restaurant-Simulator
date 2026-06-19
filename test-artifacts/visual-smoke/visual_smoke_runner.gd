extends SceneTree

const OUT_DIR := "E:/GitHub Projects/Restaurant Simulator/Restaurant Simulator/test-artifacts/visual-smoke/2026-06-19-crowd-coordinator"

var scene: Node
var smoke_camera: Camera3D

func _initialize() -> void:
	root.size = Vector2i(1600, 900)
	DirAccess.make_dir_recursive_absolute(OUT_DIR)
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
	_capture("00_overhead_start.png")
	for i in range(1, 5):
		await _frames(1200)
		_capture("%02d_overhead.png" % i)
	quit()

func _install_overhead_camera() -> void:
	smoke_camera = Camera3D.new()
	smoke_camera.name = "VisualSmokeOverheadCamera"
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
	img.save_png(OUT_DIR.path_join(name))
	print("[VisualSmoke] wrote ", OUT_DIR.path_join(name), " size=", img.get_width(), "x", img.get_height())

func _hide_node_named(n: Node, target_name: String) -> bool:
	if n.name == target_name:
		if n is Node3D:
			n.visible = false
		return true
	for child in n.get_children():
		if _hide_node_named(child, target_name):
			return true
	return false
