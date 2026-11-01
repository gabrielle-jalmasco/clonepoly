
using Godot;
using System.Threading.Tasks;

public partial class DiceController : Node2D
{
	[Export] public Sprite2D Die1;
	[Export] public Sprite2D Die2;
	[Export] public Sprite2D Die3;
	[Export] public Sprite2D Die4;
	[Export] public Sprite2D Die5;
	[Export] public Sprite2D Die6;

	private Sprite2D[] dice;

	public override void _Ready()
	{
		dice = new Sprite2D[]
		{
			Die1, Die2, Die3,
			Die4, Die5, Die6
		};

		ShowDice(1);
	}

	public void ShowDice(int number)
	{
		for (int i = 0; i < dice.Length; i++)
		{
			if (dice[i] != null)
				dice[i].Visible = (i == number - 1);
		}
	}

	public bool IsRolling { get; private set; } = false;

public async Task<int> RollDice()
{
	if (IsRolling)
		return -1;

	IsRolling = true;

	// Decide the final result
	int finalRoll = GD.RandRange(1, 6);

	// Cycle through random dice faces
	for (int i = 0; i < 15; i++)
	{
		int randomFace = GD.RandRange(1, 6);
		ShowDice(randomFace);

		// Gradually slow down the animation
		double delay = 0.04 + (i * 0.008);

		await ToSignal(
			GetTree().CreateTimer(delay),
			SceneTreeTimer.SignalName.Timeout
		);
	}

	// Show the final result
	ShowDice(finalRoll);

	GD.Print("You rolled: " + finalRoll);

	IsRolling = false;

	return finalRoll;
	}
	public async void StartCooldown()
{
	IsRolling = true;

	await ToSignal(
		GetTree().CreateTimer(5.0),
		SceneTreeTimer.SignalName.Timeout
	);

	IsRolling = false;
}

}
