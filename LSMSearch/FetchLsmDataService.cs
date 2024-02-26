using LSMSearch.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace LSMSearch;

public class FetchLsmDataService
{
	private string JsonDatafileAddress = "sv_lsm_data.json";
	private readonly IMemoryCache _cache;

	public FetchLsmDataService(IMemoryCache cache)
	{
		_cache = cache;
	}

	public async Task<DataFile?> ReadFromFileAndReturnData()
	{
		// read json file and map to Datafile
		var dataFile = _cache.Get<DataFile>("datafile");
		if (dataFile is not null) return dataFile;

		var datafileString = await File.ReadAllTextAsync(JsonDatafileAddress);
		dataFile = JsonConvert.DeserializeObject<DataFile>(datafileString);
		_cache.Set("datafile", dataFile);

		return dataFile;
	}
}