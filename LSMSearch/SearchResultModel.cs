namespace LSMSearch;

public class SearchResultModel
{
	public string Id { get; set; }
	public string EntityType { get; set; }
	public string Entity { get; set; }
	public List<MatchedProperties> MatchedPropertiesList { get; set; }
	public int TotalWeight => MatchedPropertiesList.Sum(x => x.CalculatedWeight);
}

public class MatchedProperties
{
	public string PropertyName { get; set; }
	public int CalculatedWeight { get; set; }
	public string SearchMatchType { get; set; }
}