namespace LSMSearch;

public interface ISearchEngine
{
	public Task<List<SearchResultModel>> Search(string input);
}