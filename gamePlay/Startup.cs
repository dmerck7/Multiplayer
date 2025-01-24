using Godot;
using System;

public partial class Startup : Control
{
	
	public override void _Ready()
	{
		base._Ready();
		
		// Perform game initialization here
		// Maybe check authentication status, pull in some data, initialize assets...
		// Could show loading bar
		
		GamePlayUtility.loadMainMenuScene(this.GetTree());
	}
}
