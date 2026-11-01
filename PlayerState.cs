using Godot;

public class PlayerState
{
	public string Name;
	public Color Color;
	public Sprite2D Token;
	public Label Tag;          // name tag above the token (also shows the player's money)
	public int Tile = 0;
	public int Money = 1500;
	public int JailCards = 0;  // Get Out of Jail Free cards held
	public int SkipTurns = 0;  // upcoming turns this player must skip
}
