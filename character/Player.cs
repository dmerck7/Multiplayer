using Godot;
using System;
using System.Data.Common;

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

	//public enum ANIMATIONS {JUMP_UP, WALK}
	
	[Export]
	public int currentAnimation = 1;

	[Export]
	public long player = 1;
	
	[Export]
	public Vector2 motion = new Vector2();

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
			this.currentAnimation = 0; // (int)ANIMATIONS.JUMP_UP;
			var velocity = this.Velocity;
			velocity.Y = JUMP_VELOCITY;
			this.Velocity = velocity;
			this.playerInput.jumping = false;
		}
		else if (this.IsOnFloor())
		{
			this.currentAnimation = 1; // (int)ANIMATIONS.WALK;
		
			var speed = (playerInput.running)? RUN_SPEED: SPEED;
			
			var direction = (this.Transform.Basis * - new Vector3(motion.X, 0, motion.Y)); // negative to fix movement direction
			
			var velocity = this.Velocity;
			if (direction.Length() > 0.01)
			{
				velocity.X = direction.X * speed;
				velocity.Z = direction.Z * speed;
			}
			else
			{
				velocity.X = Mathf.MoveToward(velocity.X, 0, speed);
				velocity.Z = Mathf.MoveToward(velocity.Z, 0, speed);
			}
			this.Velocity = velocity;
		}

		this.MoveAndSlide();
	}

	private void animate(int anim, double delta = 0)
	{
		this.currentAnimation = anim;

		var isRunning = (playerInput.running)? 1: 0;
		
		if (!Multiplayer.IsServer())
		{
			//GD.Print("Current anim state is "+anim);
		}
		
		// TODO: this needs some refactoring
		if (anim == 0) //(int)ANIMATIONS.JUMP_UP)
		{
			this.animationTree.Set("parameters/state/transition_request", "jump");
		}
		else if (anim == 1) // (int)ANIMATIONS.WALK)
		{
			if (!Multiplayer.IsServer() && isRunning==1)
			{
				//GD.Print(Multiplayer.GetUniqueId() + " is running");
				//this.animationPlayer.Play("Standard Run");
			} else {
				//this.animationPlayer.Play("Walking");
			}

			this.animationTree.Set("parameters/state/transition_request", "walk"); // walk
			this.animationTree.Set("parameters/Walking/blend_position", new Vector2(this.velocity.Length(), isRunning)); // blend position vector
			//this.animationTree.Set("parameters/Walking/blend_position", new Vector2(3, isRunning)); // blend position vector

		}
	}


}
