using Godot;

public enum SpaceType { Go, Street, Railroad, Utility, Chance, Chest, Tax, Jail, FreeParking, GoToJail }
public enum StreetGroup { None, Brown, LightBlue, Pink, Orange, Red, Yellow, Green, DarkBlue }

public readonly struct BoardSpace
{
	public readonly SpaceType Type;
	public readonly string Name;
	public readonly StreetGroup Group;

	public BoardSpace(SpaceType type, string name, StreetGroup group = StreetGroup.None)
	{
		Type = type;
		Name = name;
		Group = group;
	}
}

// The 40 spaces, in the order the tokens travel:
// 0 = GO (bottom-right), then right-to-left along the bottom, up the left side,
// left-to-right along the top, and down the right side.
// Rename the placeholder names to your real places later.
public static class BoardData
{
	public static readonly BoardSpace[] Spaces =
	{
		new(SpaceType.Go, "GO"),                                  // 0
		new(SpaceType.Street, "Brown 1", StreetGroup.Brown),      // 1
		new(SpaceType.Chest, "Community Chest"),                  // 2
		new(SpaceType.Street, "Brown 2", StreetGroup.Brown),      // 3
		new(SpaceType.Tax, "Income Tax"),                         // 4
		new(SpaceType.Railroad, "Railroad 1"),                    // 5
		new(SpaceType.Street, "Light Blue 1", StreetGroup.LightBlue), // 6
		new(SpaceType.Chance, "Chance"),                          // 7
		new(SpaceType.Street, "Light Blue 2", StreetGroup.LightBlue), // 8
		new(SpaceType.Street, "Light Blue 3", StreetGroup.LightBlue), // 9
		new(SpaceType.Jail, "Jail / Just Visiting"),              // 10

		new(SpaceType.Street, "Pink 1", StreetGroup.Pink),        // 11
		new(SpaceType.Utility, "Utility 1"),                      // 12
		new(SpaceType.Street, "Pink 2", StreetGroup.Pink),        // 13
		new(SpaceType.Street, "Pink 3", StreetGroup.Pink),        // 14
		new(SpaceType.Railroad, "Railroad 2"),                    // 15
		new(SpaceType.Street, "Orange 1", StreetGroup.Orange),    // 16
		new(SpaceType.Chest, "Community Chest"),                  // 17
		new(SpaceType.Street, "Orange 2", StreetGroup.Orange),    // 18
		new(SpaceType.Street, "Orange 3", StreetGroup.Orange),    // 19
		new(SpaceType.FreeParking, "Free Parking"),               // 20

		new(SpaceType.Street, "Red 1", StreetGroup.Red),          // 21
		new(SpaceType.Chance, "Chance"),                          // 22
		new(SpaceType.Street, "Red 2", StreetGroup.Red),          // 23
		new(SpaceType.Street, "Red 3", StreetGroup.Red),          // 24
		new(SpaceType.Railroad, "Railroad 3"),                    // 25
		new(SpaceType.Street, "Yellow 1", StreetGroup.Yellow),    // 26
		new(SpaceType.Street, "Yellow 2", StreetGroup.Yellow),    // 27
		new(SpaceType.Utility, "Utility 2"),                      // 28
		new(SpaceType.Street, "Yellow 3", StreetGroup.Yellow),    // 29
		new(SpaceType.GoToJail, "Go To Jail"),                    // 30

		new(SpaceType.Street, "Green 1", StreetGroup.Green),      // 31
		new(SpaceType.Street, "Green 2", StreetGroup.Green),      // 32
		new(SpaceType.Chest, "Community Chest"),                  // 33
		new(SpaceType.Street, "Green 3", StreetGroup.Green),      // 34
		new(SpaceType.Railroad, "Railroad 4"),                    // 35
		new(SpaceType.Chance, "Chance"),                          // 36
		new(SpaceType.Street, "Dark Blue 1", StreetGroup.DarkBlue), // 37
		new(SpaceType.Tax, "Luxury Tax"),                         // 38
		new(SpaceType.Street, "Dark Blue 2", StreetGroup.DarkBlue), // 39
	};

	public static string Describe(int tile)
	{
		var s = Spaces[tile];
		return s.Name == s.Type.ToString() ? s.Name : $"{s.Name} ({s.Type})";
	}

	// Prints the totals once at startup so you can check the layout matches the rules
	public static void PrintSummary()
	{
		int streets = 0, rails = 0, utils = 0, chance = 0, chest = 0, tax = 0, corners = 0;
		foreach (var s in Spaces)
		{
			switch (s.Type)
			{
				case SpaceType.Street: streets++; break;
				case SpaceType.Railroad: rails++; break;
				case SpaceType.Utility: utils++; break;
				case SpaceType.Chance: chance++; break;
				case SpaceType.Chest: chest++; break;
				case SpaceType.Tax: tax++; break;
				default: corners++; break;
			}
		}
		GD.Print($"Board: {Spaces.Length} spaces | streets {streets}, railroads {rails}, utilities {utils}, " +
				 $"chance {chance}, chest {chest}, tax {tax}, corners {corners}");
	}
}
