using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class Level01 : Node3D
{
	public const float SPAWN_RANDOM = 5.0f;
	public Node3D players;

	public override void _Ready()
	{
		base._Ready();

		GD.Print("Level ready");

		this.players = this.GetNode<Node3D>("Players");

		if (!Multiplayer.IsServer())
		{
			return ;
		}

		Multiplayer.PeerConnected += addPlayer;
		Multiplayer.PeerDisconnected += delPlayer;
		
		foreach (var id in Multiplayer.GetPeers())
		{
			this.addPlayer(id);
		}

		if (GamePlayUtility.localHostMode && !OS.HasFeature("dedicated_server"))
		{
			this.addPlayer(1);
		}
	}

	public void addPlayer(long id)
	{
		GD.Print("Add player: " + id);
 		var scene = GD.Load<PackedScene>("res://character/Player.tscn");
		var character = (Player)scene.Instantiate();
		character.player = id;

		var rng = new RandomNumberGenerator();
		var random_x = rng.RandfRange(10.0f, 15.0f);
		var random_z = rng.RandfRange(10.0f, 20.0f);
		character.Position = new Vector3(random_x, 10, random_z);

		character.Name = id.ToString();

		this.players.AddChild(character, true);
	}

	public void delPlayer(long id)
	{
		if (!this.players.HasNode(id.ToString())){
			return;
		}

		this.players.GetNode(id.ToString()).QueueFree();
	}

	public void exitTree()
	{
		Multiplayer.PeerConnected -= addPlayer;
		Multiplayer.PeerDisconnected -= delPlayer;
	}

}
