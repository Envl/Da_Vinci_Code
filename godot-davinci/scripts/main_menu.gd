extends Control

@onready var room_name_input: LineEdit = $VBoxContainer/RoomNameInput
@onready var player_name_input: LineEdit = $VBoxContainer/PlayerNameInput
@onready var create_button: Button = $VBoxContainer/CreateButton
@onready var join_button: Button = $VBoxContainer/JoinButton
@onready var player_count_option: OptionButton = $VBoxContainer/PlayerCountOption
@onready var status_label: Label = $VBoxContainer/StatusLabel
@onready var server_url_input: LineEdit = $VBoxContainer/ServerUrlInput

func _ready() -> void:
	create_button.pressed.connect(_on_create_pressed)
	join_button.pressed.connect(_on_join_pressed)

	# Setup player count options
	player_count_option.add_item("2 Players", 2)
	player_count_option.add_item("3 Players", 3)
	player_count_option.add_item("4 Players", 4)
	player_count_option.selected = 2  # Default to 4 players

	# Connect to GameManager signals
	GameManager.connected_to_server.connect(_on_connected_to_server)
	GameManager.room_state_updated.connect(_on_room_state_updated)
	GameManager.error_received.connect(_on_error_received)

func _on_create_pressed() -> void:
	var room = room_name_input.text.strip_edges()
	var name = player_name_input.text.strip_edges()

	if room.is_empty() or name.is_empty():
		status_label.text = "Please enter room and player name"
		return

	status_label.text = "Connecting..."
	GameManager.max_players = player_count_option.get_selected_id()

	var server_url = server_url_input.text.strip_edges()
	if server_url.is_empty():
		server_url = "ws://localhost:8787/room/"

	GameManager.connect_to_room(room, name, server_url)

func _on_join_pressed() -> void:
	var room = room_name_input.text.strip_edges()
	var name = player_name_input.text.strip_edges()

	if room.is_empty() or name.is_empty():
		status_label.text = "Please enter room and player name"
		return

	status_label.text = "Connecting..."

	var server_url = server_url_input.text.strip_edges()
	if server_url.is_empty():
		server_url = "ws://localhost:8787/room/"

	GameManager.connect_to_room(room, name, server_url)

func _on_connected_to_server() -> void:
	status_label.text = "Connected! Joining room..."
	GameManager.join_room()

func _on_room_state_updated() -> void:
	status_label.text = "Joined room successfully!"
	# Wait a bit before transitioning
	await get_tree().create_timer(0.5).timeout
	get_tree().change_scene_to_file("res://scenes/game_scene.tscn")

func _on_error_received(error_message: String) -> void:
	status_label.text = "Error: " + error_message
