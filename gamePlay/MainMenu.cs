using Godot;
using GodotPlugins.Game;
using System;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		base._Ready();

		GD.Print("Main Menu");
	}


	public void _on_server_mode_pressed()
	{
		GamePlayUtility.serverModeSelected = true;
		this.loadGamePlayScene(this.GetTree());
	}
	
	public void _on_client_mode_pressed()
	{
		GamePlayUtility.serverModeSelected = false;
		this.loadGamePlayScene(this.GetTree());
	}
	
	private void loadGamePlayScene(SceneTree tree)
	{
		GamePlayUtility.loadGamePlayScene(tree);
	}
	
}
