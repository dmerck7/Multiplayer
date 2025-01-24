using Godot;

public partial class Player : CharacterBody3D
{
	public const float SPEED = 5.0f;
	public const float RUN_SPEED = 8.0f;
	public const float JUMP_VELOCITY = 4.5f;
	public const float ACCELERATION = 10f;
	public const float DIRECTION_INTERPOLATE_SPEED = 1f;
	public const float MOTION_INTERPOLATE_SPEED = 10f;
	public const float ROTATION_INTERPOLATE_SPEED = 10f;

	public GamePlay gamePlayManager;
	public float gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	public PlayerInputSynchronizer playerInput;
	public AnimationTree animationTree;
	public AnimationPlayer animationPlayer;

	[Export]
	public Vector3 velocity = new Vector3();

	public enum ANIMATIONS {JUMP_UP, WALK}
	
	[Export]
	public int currentAnimation = (int)ANIMATIONS.WALK;

	[Export]
	public long player = 1;
	
	[Export]
	public Vector2 motion = new Vector2();
	public Transform3D orientation = new Transform3D();

	public override void _Ready()
	{
		base._Ready();

		this.playerInput = this.GetNode<PlayerInputSynchronizer>("PlayerInputSynchronizer");

		var hud = this.GetNode<Control>("HUD");
		this.animationTree = this.GetNode<AnimationTree>("AnimationTree");
		this.animationPlayer = this.GetNode<AnimationPlayer>("PlayerModel/AnimationPlayer");
		var gamePlayManager = this.GetTree().GetRoot().GetNode<GamePlay>("GamePlay");

		hud.Hide();
		this.animationTree.Active = true;

		orientation.Basis = this.GlobalTransform.Basis;

		if (!Multiplayer.IsServer())
		{
			SetProcess(false);
		}

		//  Tell the local game play manager this player is ready in game
		gamePlayManager.EmitSignal(GamePlay.SignalName.playerReady);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (Multiplayer.IsServer())
		{
			this.applyInput(delta);
			this.velocity = this.Velocity;
		}

		// run animations only on client or server when in "host mode"
		if (!Multiplayer.IsServer() || GamePlayUtility.localHostMode){
			this.animate(this.currentAnimation, delta);
		}

	}

	public void applyInput(double delta)
	{
		this.motion = this.motion.Lerp(playerInput.inputMotion, MOTION_INTERPOLATE_SPEED * (float)delta);

		var cameraBasis = playerInput.GetCameraRotationBasis();
		var cameraZ = cameraBasis.Z;
		var cameraX = cameraBasis.X;
		
		cameraZ.Y = 0;
		cameraZ = cameraZ.Normalized();
		cameraX.Y = 0;
		cameraX = cameraX.Normalized();

		// Add the gravity.
		if (!this.IsOnFloor())
		{
			var velocity = this.Velocity;
			velocity.Y -= (float)(this.gravity * delta);
			this.Velocity = velocity;
		}

		// TODO: this needs some refactoring
		if (!this.playerInput.jumping && this.playerInput.doJump && this.IsOnFloor())
		{
			this.playerInput.doJump = false;
			this.playerInput.jumping = true;
		} 
		else if (this.playerInput.jumping && this.IsOnFloor())
		{
			this.currentAnimation = (int)ANIMATIONS.JUMP_UP;
			var velocity = this.Velocity;
			velocity.Y = JUMP_VELOCITY;
			this.Velocity = velocity;
			this.playerInput.jumping = false;
		}
		else if (this.IsOnFloor())
		{
			var playerLookatTarget = cameraX * motion.X + cameraZ * motion.Y;
		
			if (playerLookatTarget.Length() > 0.001f)
			{
				Quaternion qFrom = orientation.Basis.GetRotationQuaternion();
				Quaternion qTo = new Transform3D().LookingAt(playerLookatTarget, Vector3.Up).Basis.GetRotationQuaternion().Normalized();

				orientation.Basis = new Basis(qFrom.Slerp(qTo, (float)delta * ROTATION_INTERPOLATE_SPEED));
			}
			
			// Using only the horizontal velocity, interpolate towards the input
			var horizontalVelocity = this.Velocity;
			horizontalVelocity.Y = 0;
		
			var speed = (playerInput.running)? RUN_SPEED: SPEED;

			// Rotates the camera basis along the X axis in the amount current X rotation, but the negative version,
			// which brings the Y axis straight back up, so we don't slow down/speed up when camera Y values 
			// change beyond UP. Camera Y values don't remain UP, so this just fixes that because we need 
			// to make sure out X and Z values are relative to an UP Y axis. 
			cameraBasis = cameraBasis.Rotated(cameraBasis.X, -cameraBasis.GetEuler().X);
			
			var direction = (cameraBasis * - new Vector3(motion.X, 0, motion.Y));
			var positionTarget = direction * speed;

			// Once under a certian velocity, just stop player. 
			if (direction.Length() < 0.01)
			{
				horizontalVelocity = Vector3.Zero;
			}
			else
			{
				horizontalVelocity = horizontalVelocity.Lerp(positionTarget, ACCELERATION * (float)delta);
			}

			this.Velocity = horizontalVelocity;
			
			// The walk handles both walk and run animations
			animate((int)ANIMATIONS.WALK, delta); 
		}

		this.SetUpDirection(Vector3.Up);
		this.MoveAndSlide();

		if (orientation.Basis.Z != Vector3.Zero)
		{
			var playerModel = this.GetNode<Node3D>("PlayerModel");

			orientation.Origin = new Vector3();
			orientation = orientation.Orthonormalized();
			
			var globalTransform = playerModel.GlobalTransform;
			globalTransform.Basis = orientation.Basis;
			playerModel.GlobalTransform = globalTransform;
		}
	}

	private void animate(int anim, double delta = 0)
	{
		this.currentAnimation = anim;

		var isRunning = (playerInput.running)? 1: 0;
		
		// TODO: this needs some refactoring
		if (anim == (int)ANIMATIONS.JUMP_UP)
		{
			this.animationTree.Set("parameters/state/transition_request", "jump");
		}
		else if (anim == (int)ANIMATIONS.WALK)
		{
			this.animationTree.Set("parameters/state/transition_request", "walk"); // walk
			this.animationTree.Set("parameters/Walking/blend_position", new Vector2(this.velocity.Length(), isRunning)); // blend position vector
		}
	}


}
