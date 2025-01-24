using Godot;

public partial class GamePlayUtility : Node
{
	public static string GAME_PLAY_SCENE = "res://gamePlay/GamePlay.tscn";
	public static string MAIN_MENU_SCENE = "res://gamePlay/MainMenu.tscn";
	public static string GAME_OVER_SCENE = "res://gamePlay/GameOver.tscn";
	
	public override void _Ready()
	{
		base._Ready();
	}

	// NOTE: Manually set this to true before running, if you want one of the debug clients to also run 
	// on the server instance (host).

	public static bool localHostMode = true;
	public static bool serverModeSelected = true;
	public static string selectedLevel = "res://level/Level01.tscn"; // This could be something that the player selects

	public static void loadGamePlayScene(SceneTree tree)
	{
		var scene = GD.Load<PackedScene>(GAME_PLAY_SCENE);
		Callable.From(()=> tree.ChangeSceneToPacked(scene)).CallDeferred();
	}

	public static void loadMainMenuScene(SceneTree tree)
	{
		var scene = GD.Load<PackedScene>(MAIN_MENU_SCENE);
		Callable.From(()=> tree.ChangeSceneToPacked(scene)).CallDeferred();
	}

	public static void loadGameOverScene(SceneTree tree)
	{
		Input.SetMouseMode(Input.MouseModeEnum.Visible); // make sure we free up mouse to allow button interaction
		var scene = GD.Load<PackedScene>(GAME_OVER_SCENE);
		Callable.From(()=> tree.ChangeSceneToPacked(scene)).CallDeferred();
	}


}
