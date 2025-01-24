using Godot;
using System;

public partial class GameOver : Control
{
	public void _on_main_menu_pressed(){
		GamePlayUtility.loadMainMenuScene(this.GetTree());
	}
}
