using Godot;
using System.Collections.Generic;

public partial class PlayerSetup : Control
{
	private const int MinPlayers = 2;
	private const int MaxPlayers = 4;

	private static readonly Color Navy = new("12233F");
	private static readonly Color Cream = new("F0E3CB");
	private static readonly Color Yellow = new("F5B000");

	private static readonly Color[] TokenColors =
	{
		new("C0392B"), new("1B3A6B"), new("2E8B57"), new("F5B000")
	};

	private int count = 4;
	private readonly List<Control> rows = new();
	private readonly List<LineEdit> inputs = new();

	private Label countLabel;

	public override void _Ready()
	{
		countLabel = GetNode<Label>("%CountLabel");

		StyleCard();
		StyleCircleButton(GetNode<Button>("%MinusButton"));
		StyleCircleButton(GetNode<Button>("%PlusButton"));
		StylePillButton(GetNode<Button>("%BackButton"), Colors.White);
		StylePillButton(GetNode<Button>("%ContinueButton"), Yellow);

		GetNode<Button>("%MinusButton").Pressed += () => SetCount(count - 1);
		GetNode<Button>("%PlusButton").Pressed += () => SetCount(count + 1);
		GetNode<Button>("%BackButton").Pressed += OnBack;
		GetNode<Button>("%ContinueButton").Pressed += OnContinue;

		BuildRows();
		SetCount(count);
	}

	// Build all rows once; we just show/hide them so typed names aren't lost.
	private void BuildRows()
	{
		var list = GetNode<VBoxContainer>("%PlayerList");

		for (int i = 0; i < MaxPlayers; i++)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			row.Alignment = BoxContainer.AlignmentMode.Center; // center swatch + input

			var swatch = new Panel
			{
				CustomMinimumSize = new Vector2(18, 18),
				SizeFlagsVertical = SizeFlags.ShrinkCenter
			};
			swatch.AddThemeStyleboxOverride("panel",
				MakeBox(TokenColors[i], Navy, 5, 2));

			var input = new LineEdit
			{
				PlaceholderText = $"Player {i + 1}",
				CustomMinimumSize = new Vector2(320, 44),
				SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
				MaxLength = 30
			};
			var inputBox = MakeBox(Cream, new Color("E0CDA5"), 8, 1);
			input.AddThemeStyleboxOverride("normal", inputBox);
			input.AddThemeStyleboxOverride("focus", MakeBox(Cream, Navy, 8, 2));
			input.AddThemeColorOverride("font_color", Navy);
			input.AddThemeColorOverride("font_placeholder_color", new Color("8A7A5C"));
			input.TextChanged += text => FitFont(input, text);

			row.AddChild(swatch);
			row.AddChild(input);
			list.AddChild(row);

			rows.Add(row);
			inputs.Add(input);
		}
	}

	private void SetCount(int value)
	{
		count = Mathf.Clamp(value, MinPlayers, MaxPlayers);
		countLabel.Text = count.ToString();

		for (int i = 0; i < rows.Count; i++)
			rows[i].Visible = i < count;

		GetNode<Button>("%MinusButton").Disabled = count == MinPlayers;
		GetNode<Button>("%PlusButton").Disabled = count == MaxPlayers;
	}
	private static void FitFont(LineEdit input, string text)
	{
	const int baseSize = 16;
	const int minSize = 10;

	Font font = input.GetThemeFont("font");
	float avail = input.CustomMinimumSize.X - 24;
	float width = font.GetStringSize(text, HorizontalAlignment.Left, -1, baseSize).X;

	int size = width <= avail
		? baseSize
		: Mathf.Max(minSize, (int)(baseSize * avail / width));

	input.AddThemeFontSizeOverride("font_size", size);
	}
	private void OnBack()
	{
		GetTree().ChangeSceneToFile("res://main_menu.tscn");
	}

	private void OnContinue()
	{
		GameSettings.PlayerNames.Clear();
		GameSettings.PlayerColors.Clear();

		for (int i = 0; i < count; i++)
		{
			string name = inputs[i].Text.Trim();
			if (name == "") name = $"Player {i + 1}";

			GameSettings.PlayerNames.Add(name);
			GameSettings.PlayerColors.Add(TokenColors[i]);
		}

		GetTree().ChangeSceneToFile("res://node_2d.tscn");
	}

	// ---------- Styling helpers ----------

	private static StyleBoxFlat MakeBox(Color bg, Color border, int radius, int borderWidth)
	{
		var sb = new StyleBoxFlat
		{
			BgColor = bg,
			BorderColor = border,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius,
			ContentMarginLeft = 12,
			ContentMarginRight = 12
		};
		sb.SetBorderWidthAll(borderWidth);
		return sb;
	}

	private void StyleCard()
	{
		var card = GetNode<PanelContainer>("Center/Card");
		var sb = MakeBox(Colors.White, Navy, 20, 2);
		sb.ShadowColor = Navy;
		sb.ShadowOffset = new Vector2(8, 8); // hard offset shadow, no blur
		sb.ShadowSize = 0;
		card.AddThemeStyleboxOverride("panel", sb);
	}

	private void StyleCircleButton(Button b)
	{
		b.CustomMinimumSize = new Vector2(44, 44);
		var box = MakeBox(Cream, Navy, 22, 2);
		b.AddThemeStyleboxOverride("normal", box);
		b.AddThemeStyleboxOverride("hover", box);
		b.AddThemeStyleboxOverride("pressed", box);
		b.AddThemeStyleboxOverride("disabled", box);
		b.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		b.AddThemeColorOverride("font_color", Navy);
		b.AddThemeColorOverride("font_disabled_color", new Color("12233F", 0.35f));
	}

	private void StylePillButton(Button b, Color bg)
	{
		b.CustomMinimumSize = new Vector2(0, 54);
		var box = MakeBox(bg, Navy, 12, 2);
		box.ContentMarginLeft = 24;
		box.ContentMarginRight = 24;
		box.ShadowColor = Navy;
		box.ShadowOffset = new Vector2(4, 4);
		box.ShadowSize = 0;
		b.AddThemeStyleboxOverride("normal", box);
		b.AddThemeStyleboxOverride("hover", box);
		b.AddThemeStyleboxOverride("pressed", box);
		b.AddThemeColorOverride("font_color", Navy);
	}
}
