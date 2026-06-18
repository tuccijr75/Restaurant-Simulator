extends SceneTree

const OUT_DIR := "E:/GitHub Projects/Restaurant Simulator/Restaurant Simulator/test-artifacts/visual-smoke/2026-06-18-restaurant-runtime"

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
	_capture("01_startup.png")
	_key(KEY_SPACE)
	await _frames(900)
	_capture("02_running_default.png")
	_key(KEY_O)
	await _frames(120)
	_capture("03_overhead.png")
	_key(KEY_1)
	await _frames(600)
	_capture("04_later_station_camera.png")
	_key(KEY_O)
	await _frames(120)
	_capture("05_later_overhead.png")
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
