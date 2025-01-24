using Godot;
using System;

public partial class PlayerInputSynchronizer : MultiplayerSynchronizer
{
	[Export]
	public Node3D cameraBase;

	[Export]
	public Node3D cameraRot;

	[Export]
	public Camera3D camera3D;

	[Export]
	public bool running = false;

	[Export]
	public bool jumping = false;

	[Export]
	public Vector2 inputMotion;

	[Export]
	public bool doJump = false;

	public const float CAMERA_MOUSE_ROTATION_SPEED = 0.001f;
	public float CAMERA_X_ROT_MIN = Mathf.DegToRad(-89.9f);
	public float CAMERA_X_ROT_MAX = Mathf.DegToRad(70);
	public const float CAMERA_UP_DOWN_MOVEMENT = 1;

	public bool paused = false;

	public NetworkManager networkManager;

	[Signal]
	public delegate void pauseEventHandler();

	[Signal]
	public delegate void unpauseEventHandler();

	public Control hud;
	
	public override void _EnterTree()
	{
		base._EnterTree();

		var player = this.GetNode<Player>("../");
	
		GD.Print("SET Authority for "+ player.player);
		this.SetMultiplayerAuthority((int)player.player);
	}

	public override void _Ready()
	{
		base._Ready();

		this.hud = this.GetNode<Control>("../HUD");

		// Disables camera on non-host server setups, or dedicated server builds
		if (!GamePlayUtility.localHostMode && Multiplayer.IsServer() || OS.HasFeature("dedicated_server"))
		{
			this.camera3D.Current = false;
		}

		if (this.GetMultiplayerAuthority() == Multiplayer.GetUniqueId() && (GamePlayUtility.localHostMode || !Multiplayer.IsServer()))
		{
			camera3D.Current = true;
			//Input.SetMouseMode(Input.MouseModeEnum.Captured);
			
			// if this is actually a host mode, we need to setup network manager as it is also a server
			if (GamePlayUtility.localHostMode)
			{
				this.networkManager = this.GetTree().GetCurrentScene().GetNode<NetworkManager>("NetworkManager");
			}
		}
		else
		{
			this.SetProcess(false);
			this.SetProcessInput(false);
			this.networkManager = this.GetTree().GetCurrentScene().GetNode<NetworkManager>("NetworkManager");
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		// handle game pause with esc key
		if (Input.IsActionJustReleased("gamePause"))
		{
			this.paused = !this.paused;
			if (this.paused)
			{
				this.inputMotion = new Vector2(0,0);
				this.running = false;
				this.pauseGame();
			}
			else
			{
				this.unpauseGame();
			}
		}

		if (!this.paused){
			this.inputMotion = Input.GetVector("moveLeft", "moveRight", "moveForward", "moveBackward");
		
			if (Input.IsActionJustPressed("ui_accept"))
			{
				Rpc("jump");
			}

			running = this.inputMotion.Length() > 0 && Input.IsActionPressed("run");
		}
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (!paused && e is InputEventMouseMotion)
		{
			InputEventMouseMotion m = (InputEventMouseMotion) e;

			this.rotateCamera(m.Relative * CAMERA_MOUSE_ROTATION_SPEED);
		}
	}

	private void rotateCamera(Vector2 move)
	{
		cameraBase.RotateY(-move.X);
		cameraBase.Orthonormalize();
		var rotation = cameraRot.Rotation;
		rotation.X = Mathf.Clamp(cameraRot.Rotation.X + (CAMERA_UP_DOWN_MOVEMENT * move.Y), CAMERA_X_ROT_MIN, CAMERA_X_ROT_MAX);
		cameraRot.Rotation = rotation;
	}

	public Basis GetCameraRotationBasis()
	{
		return cameraRot.GlobalTransform.Basis;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void jump()
	{
		// We have to use call_local to allow for hosting configurations, but we don't need to call this 
		// locally on the client since our server has authority over player movement
		if (Multiplayer.IsServer())
		{
			this.doJump = true;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void quitGame()
	{
		if (Multiplayer.IsServer())
		{
			var senderId = Multiplayer.GetRemoteSenderId();

			// Disconnection logic follows here
			this.networkManager.EmitSignal(NetworkManager.SignalName.closeClientAfterQuitSignal, senderId);
		}
	}

	// pause / mouse capture 
	public void pauseGame()
	{
		Input.SetMouseMode(Input.MouseModeEnum.Visible);
		// GetTree().Paused = true #In case you want to pause the game
		this.EmitSignal(SignalName.pause);
		this.hud.Show();
	}

	public void unpauseGame()
	{
		//Input.SetMouseMode(Input.MouseModeEnum.Captured);
		// GetTree().Paused = false
		this.EmitSignal(SignalName.unpause);
		this.hud.Hide();
	}

	public void _on_quit_game_pressed()
	{
		// need players to be able to use mouse for menu
		Input.SetMouseMode(Input.MouseModeEnum.Visible);
		Rpc("quitGame");
	}

}
	
	
