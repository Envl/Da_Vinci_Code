extends RigidBody3D

class_name Brick

# Brick properties
var brick_id: String = ""
var brick_number: int = 0  # 0-11, or -1 for joker
var brick_type: int = 0  # 0 = BLACK, 1 = WHITE
var is_open: bool = false
var owner_id: String = ""

# References
@onready var mesh_instance: MeshInstance3D = $MeshInstance3D
@onready var number_label: Label3D = $NumberLabel

# Materials
var material_black: StandardMaterial3D
var material_white: StandardMaterial3D
var material_back: StandardMaterial3D

# Signals
signal clicked(brick)

func _ready() -> void:
	# Setup materials
	material_black = StandardMaterial3D.new()
	material_black.albedo_color = Color(0.1, 0.1, 0.1)

	material_white = StandardMaterial3D.new()
	material_white.albedo_color = Color(0.9, 0.9, 0.9)

	material_back = StandardMaterial3D.new()
	material_back.albedo_color = Color(0.3, 0.2, 0.1)

	update_appearance()

	# Setup collision
	input_ray_pickable = true

func setup(p_brick_id: String, p_number: int, p_type: int) -> void:
	brick_id = p_brick_id
	brick_number = p_number
	brick_type = p_type
	update_appearance()

func update_appearance() -> void:
	if not mesh_instance:
		return

	# Show back or front
	if is_open:
		# Show number side
		if brick_type == 0:
			mesh_instance.material_override = material_black
		else:
			mesh_instance.material_override = material_white

		# Show number
		if number_label:
			number_label.visible = true
			if brick_number == -1:
				number_label.text = "-"
			else:
				number_label.text = str(brick_number)
	else:
		# Show back
		mesh_instance.material_override = material_back
		if number_label:
			number_label.visible = false

func open_brick() -> void:
	is_open = true
	update_appearance()

	# Add physics effect - flip the brick
	apply_impulse(Vector3(-5, 1, 0))
	apply_torque_impulse(Vector3(10, 0, 0))

func _input_event(_camera: Camera3D, event: InputEvent, _position: Vector3, _normal: Vector3, _shape_idx: int) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
			emit_signal("clicked", self)

func get_brick_data() -> Dictionary:
	return {
		"id": brick_id,
		"number": brick_number,
		"type": brick_type,
		"isOpen": is_open,
		"ownerId": owner_id
	}
