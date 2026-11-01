using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class BoardManager : Node2D
{
	[Export] public Sprite2D Board;
	[Export] public Sprite2D Player;           // template token (crocorrupt); duplicated once per player
	[Export] public Sprite2D Die1;
	[Export] public Sprite2D Die2;
	[Export] public Sprite2D Die3;
	[Export] public Sprite2D Die4;
	[Export] public Sprite2D Die5;
	[Export] public Sprite2D Die6;
	[Export] public DiceController Dice;
	[Export] public Camera2D Camera;
	[Export] public float TokenSize = 0;       // on-screen size in pixels; 0 = same size as the template token
	[Export] public bool ShowHud = false;      // top-left list of names + last roll (debug)
	[Export] public int NameFontSize = 26;      // size of the name tag above each token

	private static int TotalTiles => BoardData.Spaces.Length; // 40

	private readonly List<PlayerState> players = new();
	private int turn = 0;
	private bool busy = false;
	private float spread = 20f;
	private string lastInfo = "";

	private Sprite2D[] dice;
	private Label hud;

	public override void _Ready()
	{
		dice = new Sprite2D[] { Die1, Die2, Die3, Die4, Die5, Die6 };
		ShowDice(1);

		BoardData.PrintSummary();

		if (Camera != null)
			Camera.MakeCurrent();
		FitCamera();
		Callable.From(FitCamera).CallDeferred(); // run again once the window has its final size
		SpawnPlayers();
		if (ShowHud)
			BuildHud();
		UpdateHud();

		GetViewport().SizeChanged += FitCamera;
	}

	// ---------- Players ----------

	private void SpawnPlayers()
	{
		// If the board scene is run directly (F6), use 2 test players
		if (GameSettings.PlayerNames.Count == 0)
		{
			GameSettings.PlayerNames.Add("Player 1");
			GameSettings.PlayerColors.Add(new Color("C0392B"));
			GameSettings.PlayerNames.Add("Player 2");
			GameSettings.PlayerColors.Add(new Color("1B3A6B"));
		}

		int count = GameSettings.PlayerNames.Count;

		// Every token is fitted to this on-screen size
		Vector2 targetSize;
		if (TokenSize > 0)
			targetSize = new Vector2(TokenSize, TokenSize);
		else if (Player.Texture != null)
			targetSize = Player.Texture.GetSize() * Player.Scale;
		else
			targetSize = new Vector2(48, 48);

		// Never let a token be bigger than ~75% of a board tile
		float tilePx = TilePixels();
		float biggest = Mathf.Max(targetSize.X, targetSize.Y);
		if (biggest > tilePx * 0.75f)
			targetSize *= tilePx * 0.75f / biggest;

		// Offset used only when several players share one tile
		spread = tilePx * 0.4f;

		for (int i = 0; i < count; i++)
		{
			// Every player uses the crocorrupt sprite (copied from the template)
			var token = (Sprite2D)Player.Duplicate();
			Player.GetParent().AddChild(token);
			token.Visible = true;

			// Same sprite for everyone, so tint it with the player's color to tell them apart.
			// Blended with white so the croc is still recognizable.
			token.SelfModulate = Colors.White.Lerp(GameSettings.PlayerColors[i], 0.6f);

			FitToken(token, targetSize);
			AddNameTag(token, GameSettings.PlayerNames[i], GameSettings.PlayerColors[i]);

			players.Add(new PlayerState
			{
				Name = GameSettings.PlayerNames[i],
				Color = GameSettings.PlayerColors[i],
				Token = token
			});
		}

		foreach (var p in players)
			p.Token.GlobalPosition = SlotPosition(p);

		Player.Visible = false; // hide the template
	}

	// Puts the player's name in a small colored tag above the token
	private void AddNameTag(Sprite2D token, string name, Color color)
	{
		// Top-center of the sprite, in the token's local space
		Rect2 r = token.GetRect();

		// This node cancels the token's tiny scale so the label is sized in normal pixels
		var tag = new Node2D
		{
			Position = new Vector2(r.Position.X + r.Size.X / 2f, r.Position.Y),
			Scale = new Vector2(1f / token.Scale.X, 1f / token.Scale.Y)
		};
		token.AddChild(tag);

		var label = new Label
		{
			Text = name,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		label.AddThemeFontSizeOverride("font_size", NameFontSize);
		label.AddThemeColorOverride("font_color", Colors.White);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		label.AddThemeConstantOverride("outline_size", 4);

		var box = new StyleBoxFlat
		{
			BgColor = color,
			ContentMarginLeft = 8,
			ContentMarginRight = 8,
			ContentMarginTop = 2,
			ContentMarginBottom = 2
		};
		box.SetCornerRadiusAll(6);
		label.AddThemeStyleboxOverride("normal", box);

		tag.AddChild(label);

		// Center the label horizontally and sit it just above the sprite
		Vector2 size = label.GetMinimumSize();
		label.Size = size;
		label.Position = new Vector2(-size.X / 2f, -size.Y - 4f);
	}

	// Scale a token uniformly so its image fits inside the target size
	private static void FitToken(Sprite2D token, Vector2 targetSize)
	{
		if (token.Texture == null)
			return;

		Vector2 t = token.Texture.GetSize();
		float s = Mathf.Min(targetSize.X / t.X, targetSize.Y / t.Y);
		token.Scale = new Vector2(s, s);
	}

	private void BuildHud()
	{
		var layer = new CanvasLayer();
		AddChild(layer);

		hud = new Label { Position = new Vector2(16, 16) };
		hud.AddThemeFontSizeOverride("font_size", 20);
		hud.AddThemeColorOverride("font_color", Colors.White);
		hud.AddThemeColorOverride("font_outline_color", Colors.Black);
		hud.AddThemeConstantOverride("outline_size", 6);
		layer.AddChild(hud);
	}

	private void UpdateHud()
	{
		if (hud == null)
			return;

		string s = "";
		for (int i = 0; i < players.Count; i++)
			s += (i == turn ? "> " : "   ") + players[i].Name + "\n";
		if (lastInfo != "")
			s += "\n" + lastInfo;
		hud.Text = s;
	}

	// ---------- Camera & dice ----------

	private void FitCamera()
	{
		if (Camera == null)
		{
			GD.PushWarning("BoardManager: the Camera slot is empty. Assign the Camera2D in the Inspector.");
			return;
		}
		if (Board == null || Board.Texture == null)
			return;

		Vector2 boardSize = Board.Texture.GetSize() * Board.GlobalScale;
		Vector2 view = GetViewportRect().Size;

		float zoom = Mathf.Min(view.X / boardSize.X, view.Y / boardSize.Y) * 0.95f;
		GD.Print($"Camera fit -> view: {view}, board: {boardSize}, zoom: {zoom}");

		Camera.GlobalPosition = Board.GlobalPosition;
		Camera.Zoom = new Vector2(zoom, zoom);
	}

	private void ShowDice(int number)
	{
		for (int i = 0; i < dice.Length; i++)
			dice[i].Visible = (i == number - 1);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouse &&
			mouse.Pressed &&
			mouse.ButtonIndex == MouseButton.Left)
		{
			foreach (Sprite2D die in dice)
			{
				if (die == null || !die.Visible)
					continue;

				Vector2 localMouse = die.GetGlobalTransformWithCanvas()
					.AffineInverse() * mouse.Position;

				if (die.GetRect().HasPoint(localMouse))
				{
					RollDice();
					GetViewport().SetInputAsHandled();
					return;
				}
			}
		}
	}

	// ---------- Turn flow ----------

	private async void RollDice()
	{
		if (busy)
			return;
		busy = true;

		int roll = await Dice.RollDice();
		if (roll < 1)
		{
			busy = false;
			return;
		}

		var p = players[turn];
		await MoveSteps(p, roll);
		await Relayout();

		lastInfo = $"{p.Name} rolled {roll} -> tile {p.Tile}: {BoardData.Describe(p.Tile)}";
		GD.Print(lastInfo);

		turn = (turn + 1) % players.Count;
		UpdateHud();
		busy = false;
	}

	private async Task MoveSteps(PlayerState p, int steps)
	{
		for (int s = 0; s < steps; s++)
		{
			p.Tile = (p.Tile + 1) % TotalTiles;

			// Hop through the center of each tile, exactly one tile per step
			var tw = CreateTween();
			tw.TweenProperty(p.Token, "global_position", TileCenter(p.Tile), 0.12);
			await ToSignal(tw, Tween.SignalName.Finished);
		}
	}

	// After a move, settle every token into its slot (centered if alone on a tile)
	private async Task Relayout()
	{
		var tw = CreateTween().SetParallel(true);
		foreach (var q in players)
			tw.TweenProperty(q.Token, "global_position", SlotPosition(q), 0.15);
		await ToSignal(tw, Tween.SignalName.Finished);
	}

	// ---------- Board positions ----------

	// Approximate size of one side tile on screen, in pixels
	private float TilePixels()
	{
		Vector2 size = Board.Texture.GetSize();
		Vector2 sc = Board.GlobalScale.Abs();
		float w = size.X * 0.78f / 9f * sc.X;   // (1 - 2 * 0.11) / 9
		float h = size.Y * 0.74f / 9f * sc.Y;   // (1 - 2 * 0.13) / 9
		return Mathf.Min(w, h);
	}

	// Where a player's token sits: the tile center if alone, a small offset if sharing
	private Vector2 SlotPosition(PlayerState p)
	{
		var sharing = players.FindAll(o => o.Tile == p.Tile);
		int n = sharing.Count;
		if (n <= 1)
			return TileCenter(p.Tile);

		int rank = sharing.IndexOf(p);
		Vector2 off = n == 2
			? new Vector2((rank - 0.5f) * spread, 0)
			: new Vector2((rank % 2 - 0.5f) * spread, (rank / 2 - 0.5f) * spread);

		return TileCenter(p.Tile) + off;
	}

	private Vector2 TileCenter(int tile)
	{
		Vector2 size = Board.Texture.GetSize();

		float left = -size.X / 2.0f;
		float top = -size.Y / 2.0f;
		float right = size.X / 2.0f;
		float bottom = size.Y / 2.0f;

		// Corners are larger, so use 9 spaces between corners.
		float cornerW = size.X * 0.11f;
		float cornerH = size.Y * 0.13f;
		float sideW = (size.X - 2 * cornerW) / 9.0f;
		float sideH = (size.Y - 2 * cornerH) / 9.0f;

		Vector2 localPos;

		if (tile == 0)
			localPos = new Vector2(right - cornerW / 2, bottom - cornerH / 2);
		else if (tile <= 9)
			localPos = new Vector2(right - cornerW - (tile - 0.5f) * sideW, bottom - cornerH / 2);
		else if (tile == 10)
			localPos = new Vector2(left + cornerW / 2, bottom - cornerH / 2);
		else if (tile <= 19)
			localPos = new Vector2(left + cornerW / 2, bottom - cornerH - (tile - 10 - 0.5f) * sideH);
		else if (tile == 20)
			localPos = new Vector2(left + cornerW / 2, top + cornerH / 2);
		else if (tile <= 29)
			localPos = new Vector2(left + cornerW + (tile - 20 - 0.5f) * sideW, top + cornerH / 2);
		else if (tile == 30)
			localPos = new Vector2(right - cornerW / 2, top + cornerH / 2);
		else
			localPos = new Vector2(right - cornerW / 2, top + cornerH + (tile - 30 - 0.5f) * sideH);

		return Board.ToGlobal(localPos);
	}
}
