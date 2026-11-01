using Godot;
using System.Threading.Tasks;

public partial class CardPopup : CanvasLayer
{
	private static readonly Color Navy = new("12233F");
	private static readonly Color Yellow = new("F5B000");

	private Control root;
	private ColorRect dim;
	private VBoxContainer holder;
	private Label whoLabel;
	private TextureRect cardImage;
	private PanelContainer fallbackPanel;
	private Label fallbackLabel;
	private Button okButton;

	public override void _Ready()
	{
		Layer = 10;

		root = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(root);
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		// Dark overlay: also blocks clicks from reaching the dice while a card is open
		dim = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.6f),
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		root.AddChild(dim);
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		root.AddChild(center);
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		holder = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		holder.AddThemeConstantOverride("separation", 14);
		center.AddChild(holder);

		whoLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		whoLabel.AddThemeFontSizeOverride("font_size", 22);
		whoLabel.AddThemeColorOverride("font_color", Colors.White);
		whoLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		whoLabel.AddThemeConstantOverride("outline_size", 6);

		cardImage = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};

		// Used only when a card image is missing
		fallbackPanel = new PanelContainer { Visible = false, CustomMinimumSize = new Vector2(480, 260) };
		fallbackPanel.AddThemeStyleboxOverride("panel", MakeBox(Colors.White, Navy, 16, 4));
		var margin = new MarginContainer();
		foreach (var side in new[] { "left", "top", "right", "bottom" })
			margin.AddThemeConstantOverride("margin_" + side, 24);
		fallbackPanel.AddChild(margin);
		fallbackLabel = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		fallbackLabel.AddThemeFontSizeOverride("font_size", 24);
		fallbackLabel.AddThemeColorOverride("font_color", Navy);
		margin.AddChild(fallbackLabel);

		okButton = new Button
		{
			Text = "OK",
			CustomMinimumSize = new Vector2(180, 46),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
		};
		var okBox = MakeBox(Yellow, Navy, 10, 2);
		okButton.AddThemeStyleboxOverride("normal", okBox);
		okButton.AddThemeStyleboxOverride("hover", okBox);
		okButton.AddThemeStyleboxOverride("pressed", okBox);
		okButton.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		okButton.AddThemeColorOverride("font_color", Navy);

		holder.AddChild(whoLabel);
		holder.AddChild(cardImage);
		holder.AddChild(fallbackPanel);
		holder.AddChild(okButton);
	}

	public async Task ShowCard(CardInfo c, string playerName)
	{
		whoLabel.Text = playerName + (c.Kind == CardKind.Chance
			? " drew a Chance card"
			: " drew a Community Chest card");

		Texture2D tex = ResourceLoader.Exists(c.ImagePath) ? GD.Load<Texture2D>(c.ImagePath) : null;
		if (tex != null)
		{
			Vector2 view = GetViewport().GetVisibleRect().Size;
			float w = Mathf.Min(560f, view.X * 0.85f);
			float h = w * tex.GetHeight() / tex.GetWidth();

			cardImage.Texture = tex;
			cardImage.CustomMinimumSize = new Vector2(w, h);
			cardImage.Visible = true;
			fallbackPanel.Visible = false;
		}
		else
		{
			fallbackLabel.Text = c.Text;
			fallbackPanel.Visible = true;
			cardImage.Visible = false;
		}

		// Start state for the pop-in
		root.Visible = true;
		dim.Modulate = new Color(1, 1, 1, 0);
		holder.Scale = new Vector2(0.1f, 0.1f);
		holder.Rotation = Mathf.DegToRad(-12);

		// Wait one frame so the layout has its real size, then scale/rotate around the center
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		holder.PivotOffset = holder.Size / 2;

		var tw = CreateTween().SetParallel(true);
		tw.TweenProperty(dim, "modulate:a", 1.0f, 0.2);
		tw.TweenProperty(holder, "scale", Vector2.One, 0.45)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tw.TweenProperty(holder, "rotation", 0.0f, 0.45)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		await ToSignal(tw, Tween.SignalName.Finished);

		// Wait for the player to press OK
		await ToSignal(okButton, BaseButton.SignalName.Pressed);

		var tw2 = CreateTween().SetParallel(true);
		tw2.TweenProperty(holder, "scale", new Vector2(0.1f, 0.1f), 0.25)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
		tw2.TweenProperty(dim, "modulate:a", 0.0f, 0.25);
		await ToSignal(tw2, Tween.SignalName.Finished);

		root.Visible = false;
	}

	private static StyleBoxFlat MakeBox(Color bg, Color border, int radius, int borderWidth)
	{
		var sb = new StyleBoxFlat
		{
			BgColor = bg,
			BorderColor = border,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius
		};
		sb.SetBorderWidthAll(borderWidth);
		return sb;
	}
}