
using Godot;

public partial class BoardManager : Node2D
{
	[Export] public Sprite2D Board;
	[Export] public Sprite2D Player;
	[Export] public Sprite2D Die1;
	[Export] public Sprite2D Die2;
	[Export] public Sprite2D Die3;
	[Export] public Sprite2D Die4;
	[Export] public Sprite2D Die5;
	[Export] public Sprite2D Die6;
	[Export] public DiceController Dice;


	private int currentTile = 0;
	private const int TotalTiles = 40;
	private const int GridSize = 11;
	
private Sprite2D[] dice;
	public override void _Ready()
	{	
		
		dice = new Sprite2D[]
{
	Die1, Die2, Die3, Die4, Die5, Die6
};

ShowDice(1);
MoveToTile(currentTile);
	}
	
	private void ShowDice(int number)
{
	for (int i = 0; i < dice.Length; i++)
	{
		dice[i].Visible = (i == number - 1);
	}
}

	public override void _UnhandledInput(InputEvent @event)
{
	if (@event is InputEventMouseButton mouse &&
		mouse.Pressed &&
		mouse.ButtonIndex == MouseButton.Left)
	{
		GD.Print("Mouse clicked!");

		foreach (Sprite2D die in dice)
		{
			if (die == null || !die.Visible)
				continue;

			Vector2 localMouse = die.GetGlobalTransformWithCanvas()
				.AffineInverse() * mouse.Position;

			if (die.GetRect().HasPoint(localMouse))
			{
				GD.Print("Dice clicked!");
				RollDice();
				GetViewport().SetInputAsHandled();
				return;
			}
		}
	}
}


	

private void MoveToTile(int tile)
{
	if (Board == null || Player == null || Board.Texture == null)
		return;

	// Board texture dimensions in local coordinates
	Vector2 size = Board.Texture.GetSize();

	float left = -size.X / 2.0f;
	float top = -size.Y / 2.0f;
	float right = size.X / 2.0f;
	float bottom = size.Y / 2.0f;

	// Center positions of the four sides.
	// Corners are larger, so use 9 spaces between corners.
	float cornerW = size.X * 0.11f;
	float cornerH = size.Y * 0.13f;
	float sideW = (size.X - 2 * cornerW) / 9.0f;
	float sideH = (size.Y - 2 * cornerH) / 9.0f;

	Vector2 localPos;

	if (tile == 0)
	{
		// GO: bottom-right corner
		localPos = new Vector2(right - cornerW / 2, bottom - cornerH / 2);
	}
	else if (tile <= 9)
	{
		// Bottom side: move right to left
		localPos = new Vector2(
			right - cornerW - (tile - 0.5f) * sideW,
			bottom - cornerH / 2
		);
	}
	else if (tile == 10)
	{
		// Bottom-left corner
		localPos = new Vector2(left + cornerW / 2, bottom - cornerH / 2);
	}
	else if (tile <= 19)
	{
		// Left side: move bottom to top
		localPos = new Vector2(
			left + cornerW / 2,
			bottom - cornerH - (tile - 10 - 0.5f) * sideH
		);
	}
	else if (tile == 20)
	{
		// Top-left corner
		localPos = new Vector2(left + cornerW / 2, top + cornerH / 2);
	}
	else if (tile <= 29)
	{
		// Top side: move left to right
		localPos = new Vector2(
			left + cornerW + (tile - 20 - 0.5f) * sideW,
			top + cornerH / 2
		);
	}
	else if (tile == 30)
	{
		// Top-right corner
		localPos = new Vector2(right - cornerW / 2, top + cornerH / 2);
	}
	else
	{
		// Right side: move top to bottom
		localPos = new Vector2(
			right - cornerW / 2,
			top + cornerH + (tile - 30 - 0.5f) * sideH
		);
	}

	// Convert board-local position into world position
	Player.GlobalPosition = Board.ToGlobal(localPos);
}

private async void RollDice()
{
	int roll = await Dice.RollDice();

	if (roll < 1)
		return;

	currentTile = (currentTile + roll) % TotalTiles;
	MoveToTile(currentTile);
	Dice.StartCooldown();
}
}
