extends Node3D

# References
@onready var table: Node3D = $Table
@onready var camera: Camera3D = $Camera3D
@onready var ui: Control = $UI
@onready var brick_container: Node3D = $BrickContainer
@onready var player_areas: Node3D = $PlayerAreas

# UI elements
@onready var status_label: Label = $UI/StatusLabel
@onready var start_button: Button = $UI/StartButton
@onready var ready_button: Button = $UI/ReadyButton
@onready var guess_panel: Panel = $UI/GuessPanel
@onready var guess_container: VBoxContainer = $UI/GuessPanel/VBoxContainer

# Game state
var bricks: Dictionary = {}  # brick_id -> Brick node
var brick_scene: PackedScene
var selected_brick: Brick = null
var guessing_brick: Brick = null

func _ready() -> void:
	# Load brick scene
	brick_scene = preload("res://scenes/brick.tscn")

	# Setup UI
	start_button.pressed.connect(_on_start_pressed)
	ready_button.pressed.connect(_on_ready_pressed)
	guess_panel.visible = false

	# Connect to GameManager signals
	GameManager.room_state_updated.connect(_on_room_state_updated)
	GameManager.game_started.connect(_on_game_started)
	GameManager.brick_picked.connect(_on_brick_picked)
	GameManager.brick_opened.connect(_on_brick_opened)
	GameManager.turn_changed.connect(_on_turn_changed)
	GameManager.game_over.connect(_on_game_over)
	GameManager.player_joined.connect(_on_player_joined)

	# Initial setup
	_on_room_state_updated()

func _on_room_state_updated() -> void:
	# Update UI based on room state
	var player_count = GameManager.players.size()
	status_label.text = "Players: %d/%d" % [player_count, GameManager.max_players]

	# Show start button only for first player
	if GameManager.players.size() > 0:
		var first_player = GameManager.players[0]
		start_button.visible = first_player.get("id") == GameManager.player_id

	# Create bricks if needed
	if bricks.is_empty() and not GameManager.unoccupied_bricks.is_empty():
		_create_bricks()

func _on_game_started() -> void:
	status_label.text = "Game Started!"
	start_button.visible = false
	_update_game_state()

func _on_player_joined(player_data: Dictionary) -> void:
	var player_name = player_data.get("name", "Player")
	status_label.text = player_name + " joined!"

func _on_start_pressed() -> void:
	GameManager.start_game()
	start_button.visible = false

func _on_ready_pressed() -> void:
	GameManager.ready()
	ready_button.visible = false

func _on_brick_picked(player_id: String, brick_data: Dictionary) -> void:
	var brick_id = brick_data.get("brickId", "")
	if bricks.has(brick_id):
		var brick = bricks[brick_id]
		brick.owner_id = player_id

		# Move brick to player's area
		_move_brick_to_player(brick, player_id)

	_update_game_state()

func _on_brick_opened(player_id: String, brick_id: String) -> void:
	if bricks.has(brick_id):
		var brick = bricks[brick_id]
		brick.open_brick()

	_update_game_state()

func _on_turn_changed(player_id: String) -> void:
	if player_id == GameManager.player_id:
		status_label.text = "Your turn!"
	else:
		var player = GameManager.get_player_by_id(player_id)
		var player_name = player.get("name", "Player")
		status_label.text = player_name + "'s turn"

	_update_game_state()

func _on_game_over(winner_id: String) -> void:
	if winner_id == GameManager.player_id:
		status_label.text = "You Win!"
	else:
		var winner = GameManager.get_player_by_id(winner_id)
		var winner_name = winner.get("name", "Player")
		status_label.text = winner_name + " wins!"

func _create_bricks() -> void:
	# Create all 26 bricks
	var brick_spacing = 1.5
	var bricks_per_row = 13

	for i in range(26):
		var brick_data: Dictionary
		var brick_number: int
		var brick_type: int

		# Determine brick properties
		if i < 13:
			# Black bricks (0-11, joker)
			brick_type = 0
			if i < 12:
				brick_number = i
			else:
				brick_number = -1  # Joker
		else:
			# White bricks (0-11, joker)
			brick_type = 1
			if i < 25:
				brick_number = i - 13
			else:
				brick_number = -1  # Joker

		# Create brick instance
		var brick_instance: Brick = brick_scene.instantiate()
		var brick_id = "brick_" + str(i)
		brick_instance.setup(brick_id, brick_number, brick_type)
		brick_instance.clicked.connect(_on_brick_clicked)

		# Position on table
		var row = i / bricks_per_row
		var col = i % bricks_per_row
		brick_instance.position = Vector3(
			(col - bricks_per_row / 2.0) * brick_spacing,
			2.0 + randf() * 2.0,  # Random height for scatter effect
			(row - 1) * brick_spacing
		)

		# Add random rotation
		brick_instance.rotation = Vector3(
			randf() * PI,
			randf() * TAU,
			randf() * PI
		)

		brick_container.add_child(brick_instance)
		bricks[brick_id] = brick_instance

func _move_brick_to_player(brick: Brick, player_id: String) -> void:
	# Find player's area index
	var player_index = 0
	for i in range(GameManager.players.size()):
		if GameManager.players[i].get("id") == player_id:
			player_index = i
			break

	# Calculate position based on player index
	var angle = (TAU / GameManager.max_players) * player_index
	var radius = 8.0
	var target_pos = Vector3(
		cos(angle) * radius,
		1.0,
		sin(angle) * radius
	)

	# Animate brick movement
	var tween = create_tween()
	tween.tween_property(brick, "position", target_pos, 0.5)
	tween.tween_property(brick, "rotation", Vector3.ZERO, 0.3)

func _on_brick_clicked(brick: Brick) -> void:
	if not GameManager.is_my_turn():
		return

	match GameManager.player_state:
		GameManager.PlayerState.PICK_BRICK:
			if brick.owner_id.is_empty():
				# Pick this brick
				GameManager.pick_brick(brick.brick_id)

		GameManager.PlayerState.GUESS_BRICK:
			if brick.owner_id != GameManager.player_id and not brick.is_open:
				# Start guessing this brick
				guessing_brick = brick
				_show_guess_panel(brick)

		GameManager.PlayerState.OPEN_BRICK:
			if brick.owner_id == GameManager.player_id and not brick.is_open:
				# Open own brick
				GameManager.open_brick(brick.brick_id)

func _show_guess_panel(brick: Brick) -> void:
	guess_panel.visible = true

	# Clear previous guess buttons
	for child in guess_container.get_children():
		child.queue_free()

	# Add guess buttons for 0-11
	for i in range(12):
		var button = Button.new()
		button.text = str(i)
		button.pressed.connect(_on_guess_number.bind(i))
		guess_container.add_child(button)

	# Add cancel button
	var cancel_button = Button.new()
	cancel_button.text = "Cancel"
	cancel_button.pressed.connect(_on_guess_cancel)
	guess_container.add_child(cancel_button)

func _on_guess_number(number: int) -> void:
	if guessing_brick:
		GameManager.guess_brick(guessing_brick.brick_id, number)
		guess_panel.visible = false
		guessing_brick = null

func _on_guess_cancel() -> void:
	guess_panel.visible = false
	guessing_brick = null

func _update_game_state() -> void:
	# Update UI based on current game state
	ready_button.visible = GameManager.current_state == GameManager.GameState.ADJUST

	# Update status based on player state
	if GameManager.is_my_turn():
		match GameManager.player_state:
			GameManager.PlayerState.PICK_BRICK:
				status_label.text = "Pick a brick from the table"
			GameManager.PlayerState.PLACE_JOKER:
				status_label.text = "Place your joker"
			GameManager.PlayerState.GUESS_BRICK:
				status_label.text = "Guess an opponent's brick"
			GameManager.PlayerState.OPEN_BRICK:
				status_label.text = "Open one of your bricks"
			GameManager.PlayerState.WATCHING:
				status_label.text = "You are out - watching"
