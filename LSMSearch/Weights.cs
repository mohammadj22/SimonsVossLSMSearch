namespace LSMSearch;

public static class Weights
{
	public static int FullMatchCoefficient = 10;
	public static Dictionary<string, int> BuildingModelWeights = new()
	{
		{ "ShortCut", 7 },
		{ "Name", 9 },
		{ "Description", 5 }
	};

	public static Dictionary<string, int> LockModelWeights = new()
	{
		{ "Type", 3 },
		{ "Name", 10 },
		{ "SerialNumber", 8 },
		{ "Floor", 6 },
		{ "RoomNumber", 6 },
		{ "Description", 6 }
	};
	
	public static Dictionary<string, int> BuildingLock = new ()
	{
		{ "ShortCut", 5 },
		{ "Name", 8 },
	};
	
	public static Dictionary<string, int> Group = new()
	{
		{ "Name", 9 },
		{ "Description", 5 }
	};
	
	public static Dictionary<string, int> Media = new()
	{
		{ "Type", 3 },
		{ "Owner", 10 },
		{ "SerialNumber", 8 },
		{ "Description", 6 }
	};

	public static Dictionary<string, int> GetModelWeightsDictionary(string modelName)
	{
		return modelName switch
		{
			"Buildings" => BuildingModelWeights,
			"Locks" => LockModelWeights,
			"BuildingLock" => BuildingLock,
			"Groups" => Group,
			"Media" => Media,
			_ => throw new ArgumentException("Model not found")
		};
	}
}