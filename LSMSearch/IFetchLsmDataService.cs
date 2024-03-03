using LSMSearch.Models;

namespace LSMSearch;

public interface IFetchLsmDataService
{
	Task<DataFile?> ReadFromFileAndReturnData();
}