using Godot;
using System.Collections.Generic;

public enum CardKind { Chance, Chest }

// One card. Set only the effects the card has; everything else stays at its default.
public class CardInfo
{
	public CardKind Kind;
	public string Id;          // image file name without extension, e.g. "chance_01"
	public string Text;        // shown only if the image is missing

	public int Money;          // + collect from the bank, - pay the bank
	public int MoveBy;         // + forward, - backward (moving backward never pays the GO salary)
	public string MoveTo;      // advance to the space with this name (pays the GO salary if you pass GO)
	public bool GoToJail;
	public bool JailCard;      // keep a Get Out of Jail Free card
	public int EachPlayer;     // + collect from every other player, - pay every other player
	public int ToPool;         // pay into the Free Parking pool
	public int SkipTurns;      // miss this many of your next turns

	public CardInfo(CardKind kind, string id, string text)
	{
		Kind = kind;
		Id = id;
		Text = text;
	}

	public string ImagePath =>
		$"res://cards/{(Kind == CardKind.Chance ? "chance" : "chest")}/{Id}.png";
}

public static class CardDeck
{
	// Same order as the pages of your PDF (chance_01 = first Chance page, and so on)
	private static readonly List<CardInfo> chanceAll = new()
	{
		new(CardKind.Chance, "chance_01", "Traffic sa EDSA! Advance to GO (Collect P200).") { MoveTo = "GO" },
		new(CardKind.Chance, "chance_02", "Na-hulidap ka ng enforcer. Pay a fine of P150.") { Money = -150 },
		new(CardKind.Chance, "chance_03", "Nakapulot ka ng P500 sa bulsa ng lumang pantalon. Collect P500.") { Money = 500 },
		new(CardKind.Chance, "chance_04", "Biglang tumaas ang presyo ng sibuyas at bigas. Pay P50.") { Money = -50 },
		new(CardKind.Chance, "chance_05", "Advance to Ayala Avenue. If you pass GO, collect P200.") { MoveTo = "Ayala Avenue, Makati" },
		new(CardKind.Chance, "chance_06", "Napagastos sa unexpected Pa-Merienda ng tropa. Pay P100.") { Money = -100 },
		new(CardKind.Chance, "chance_07", "Nanalo ka sa barangay raffle. Collect P300.") { Money = 300 },
		new(CardKind.Chance, "chance_08", "Na-flat-an ng gulong ang dyip mo sa gitna ng baha. Pay P175.") { Money = -175 },
		new(CardKind.Chance, "chance_09", "Bank error in your favor! Collect P150.") { Money = 150 },
		new(CardKind.Chance, "chance_10", "Bank error in your favor! Collect P150.") { Money = 150 },
		new(CardKind.Chance, "chance_11", "Go directly to Jail. Do not pass GO.") { GoToJail = true },
		new(CardKind.Chance, "chance_12", "Napili kang tumayong Ninong/Ninang sa binyag. Pay P150.") { Money = -150 },
		new(CardKind.Chance, "chance_13", "Bumili ka ng Budol items online. Pay P100.") { Money = -100 },
		new(CardKind.Chance, "chance_14", "Your 13th Month Pay has arrived! Collect P300.") { Money = 300 },
		new(CardKind.Chance, "chance_15", "Nagkamali ang sukli sa sari-sari store. Collect P20.") { Money = 20 },
		new(CardKind.Chance, "chance_16", "Speeding ticket along C5. Pay P50.") { Money = -50 },
		new(CardKind.Chance, "chance_17", "Get Out of Jail Free card. Keep this card until needed.") { JailCard = true },
	};

	private static readonly List<CardInfo> chestAll = new()
	{
		new(CardKind.Chest, "chest_01", "Bayanihan Spirit: every player gives you P10.") { EachPlayer = 10 },
		new(CardKind.Chest, "chest_02", "Tuwing Araw ng Sahod. Collect P200 from the Bank.") { Money = 200 },
		new(CardKind.Chest, "chest_03", "Nagkasakit dahil sa init ng panahon: pay P100.") { Money = -100 },
		new(CardKind.Chest, "chest_04", "Graduation ng Anak: collect P150 from the Bank.") { Money = 150 },
		new(CardKind.Chest, "chest_05", "Fiesta sa Nayon! Pay P50 to every player.") { EachPlayer = -50 },
		new(CardKind.Chest, "chest_06", "School Funds, ambagan: pay P50.") { Money = -50 },
		new(CardKind.Chest, "chest_07", "Nagpautang ka sa kaibigan na Makakalimutin: pay P50 fee.") { Money = -50 },
		new(CardKind.Chest, "chest_08", "May nag-padala ng Balikbayan Box! Collect 4.") { Money = 4 },
		new(CardKind.Chest, "chest_09", "Nanalo sa TV Game Show. Collect P100.") { Money = 100 },
		new(CardKind.Chest, "chest_10", "Nagparehistro ng sasakyan sa LTO. Pay P100.") { Money = -100 },
		new(CardKind.Chest, "chest_11", "Hiningan ka ng 2 valid ID: pay P150, move back 2 blocks.") { Money = -150, MoveBy = -2 },
		new(CardKind.Chest, "chest_12", "Pumasa sa Board Exam. Collect P100.") { Money = 100 },
		new(CardKind.Chest, "chest_13", "Nagpalinis ng aircon. Pay P1000.") { Money = -1000 },
		new(CardKind.Chest, "chest_14", "May dumating na bisita na may pasalubong. Collect P50.") { Money = 50 },
		new(CardKind.Chest, "chest_15", "Income tax refund. Collect P500.") { Money = 500 },
		new(CardKind.Chest, "chest_16", "Nag-donate para sa biktima ng Bagyo. Pay P50 to the Free Parking pool.") { ToPool = 50 },
		new(CardKind.Chest, "chest_17", "Nag-donate para sa biktima ng Bagyo. Pay P50 to the Free Parking pool.") { ToPool = 50 },
		new(CardKind.Chest, "chest_18", "Nag-donate para sa biktima ng Bagyo. Move 2 blocks.") { MoveBy = 2 },
		new(CardKind.Chest, "chest_19", "Credit Card: pay P200 in bills. Don't move for 2 rounds.") { Money = -200, SkipTurns = 2 },
	};

	private static readonly Queue<CardInfo> chanceQ = new();
	private static readonly Queue<CardInfo> chestQ = new();

	public static void Reset()
	{
		chanceQ.Clear();
		chestQ.Clear();
	}

	public static CardInfo Draw(CardKind kind)
	{
		var q = kind == CardKind.Chance ? chanceQ : chestQ;
		if (q.Count == 0)
			Refill(q, kind == CardKind.Chance ? chanceAll : chestAll);
		return q.Dequeue();
	}

	// Shuffles the whole deck; every card shows up once before any card repeats
	private static void Refill(Queue<CardInfo> q, List<CardInfo> all)
	{
		var copy = new List<CardInfo>(all);
		for (int i = copy.Count - 1; i > 0; i--)
		{
			int j = GD.RandRange(0, i);
			(copy[i], copy[j]) = (copy[j], copy[i]);
		}
		foreach (var c in copy)
			q.Enqueue(c);
	}
}