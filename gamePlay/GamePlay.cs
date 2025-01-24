using Godot;
using System;

public partial class GamePlay : Node
{
	[Signal]
	public delegate void playerReadyEventHandler();
	
	public override void _Ready()
	{
		base._Ready();
		
		playerReady += handlePlayerReady;
	}
	
	public void handlePlayerReady() 
	{
		var ui = this.GetNode<Control>("UI");
		ui.Hide();
	}
	
}
