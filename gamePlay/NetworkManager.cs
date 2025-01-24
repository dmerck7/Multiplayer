using Godot;

public partial class NetworkManager : Node
{
	public const int SERVER_PORT = 8080;
	public const string SERVER_IP = "127.0.0.1";

	public ENetMultiplayerPeer serverPeer;
	public ENetMultiplayerPeer clientPeer;

	[Signal]
	public delegate void closeClientAfterQuitSignalEventHandler(int peerId);

	public override void _Ready()
	{
		base._Ready();

		if (OS.HasFeature("dedicated_server") || GamePlayUtility.serverModeSelected)
		{
			this._on_host_pressed();
			//close_client_after_quit_signal.connect(_close_client_after_quit);

			closeClientAfterQuitSignal += _close_client_after_quit;
			
			// We still need this callback in case the host client quits
			if (GamePlayUtility.localHostMode)
			{
				Multiplayer.ServerDisconnected += _on_server_disconnected; // only emitted on clients
			}
		}	
		else
		{
			this._on_client_pressed();
			Multiplayer.ServerDisconnected += _on_server_disconnected; // only emitted on clients
		}
	}

	public void _on_host_pressed()
	{
		GD.Print("host pressed");
		this.serverPeer = new ENetMultiplayerPeer();
		this.serverPeer.CreateServer(SERVER_PORT);
		Multiplayer.MultiplayerPeer = this.serverPeer;
		this.startGame();
	}

	public void _on_client_pressed()
	{
		GD.Print("client pressed");
		this.clientPeer = new ENetMultiplayerPeer();
		this.clientPeer.CreateClient(SERVER_IP, SERVER_PORT);
		Multiplayer.MultiplayerPeer = this.clientPeer;
		this.startGame();
	}

	private void startGame()
	{
		GD.Print("startGame");
		if (Multiplayer.IsServer())
		{
			var scene = GD.Load<PackedScene>(GamePlayUtility.selectedLevel);
			Callable.From(() => changeLevel(scene)).CallDeferred();
		}
	}

	private void changeLevel(PackedScene scene)
	{
		var level = this.GetNode<Node>("../Level");
		foreach(var child in level.GetChildren())
		{
			level.RemoveChild(child);
			child.QueueFree();
		}

		level.AddChild(scene.Instantiate());
	}

	// Should be only used server side
	private void _close_client_after_quit(int peerId)
	{
		if (Multiplayer.IsServer())
		{
			GD.Print("_close_client_after_quit: disconnecting peer: " + peerId);
			if (peerId == 1) // for when a peer is in host mode
			{
				serverPeer.Close();
			}
		}
		else
		{
			serverPeer.DisconnectPeer(peerId);
		}
	}

	// This is hit when a client disconnects, on the client that disconnected. Also, if the server crashes
	// or disconnects, it is also hit
	private void _on_server_disconnected()
	{
		// client side move to game over state
		GamePlayUtility.loadGameOverScene(this.GetTree());
	}
}
