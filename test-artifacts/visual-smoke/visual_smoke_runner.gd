extends SceneTree

const OUT_DIR := "E:/GitHub Projects/Restaurant Simulator/Restaurant Simulator/test-artifacts/visual-smoke/2026-06-18-kitchen-layout"

var scene: Node

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
	var cam := Camera3D.new()
	cam.name = "WideKitchenSmokeCamera"
	cam.fov = 72.0
	cam.position = Vector3(-6.8, 11.8, 1.4)
	scene.add_child(cam)
	cam.look_at(Vector3(2.6, 0.4, -6.15), Vector3.UP)
	cam.current = true
	await _frames(30)
	_capture("01_wide_kitchen_layout.png")
	_key(KEY_SPACE)
	await _frames(420)
	_capture("02_wide_kitchen_running.png")
	_key(KEY_O)
	await _frames(120)
	_capture("03_overhead.png")
	quit()

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
