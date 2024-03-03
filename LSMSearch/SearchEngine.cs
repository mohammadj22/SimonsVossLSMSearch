using System.Collections;
using LSMSearch.Models;
using Newtonsoft.Json;

namespace LSMSearch;

public class SearchEngine : ISearchEngine
{
	private readonly IFetchLsmDataService _fetchLsmDataService;

	public SearchEngine(IFetchLsmDataService fetchLsmDataService)
	{
		_fetchLsmDataService = fetchLsmDataService;
	}

	public async Task<List<SearchResultModel>> Search(string input)
	{
		var data = await _fetchLsmDataService.ReadFromFileAndReturnData();
		var searchResult = new List<SearchResultModel>();

		foreach (var modelDataList in data.GetType().GetProperties())
		{
			var modelName = modelDataList.Name;
			var modelWeightsDictionary = Weights.GetModelWeightsDictionary(modelName);
			if (modelDataList.GetValue(data) is IEnumerable entities)
			{
				foreach (var entityObject in entities)
				{
					if (entityObject is IEntity entity)
					{
						var matchedProperties = new List<MatchedProperties>();
						var searchResultModel = new SearchResultModel()
						{
							Id = entity.Id,
							EntityType = entity.GetType().Name,
							MatchedPropertiesList = matchedProperties,
							Entity = JsonConvert.SerializeObject(entity)
						};

						foreach (var property in entity.GetType().GetProperties())
						{
							var propertyValue = property.GetValue(entity)?.ToString();
							if (propertyValue != null && property.Name != "Id")
							{
								if (propertyValue == input)
								{
									matchedProperties.Add(new MatchedProperties()
									{
										PropertyName = property.Name,
										CalculatedWeight = Weights.FullMatchCoefficient * modelWeightsDictionary[property.Name],
										SearchMatchType = SearchMatchType.FullMatch.ToString(),
									});
								}
								else if (propertyValue.Contains(input))
								{
									matchedProperties.Add(new MatchedProperties()
									{
										PropertyName = property.Name,
										CalculatedWeight = modelWeightsDictionary[property.Name],
										SearchMatchType = SearchMatchType.PartialMatch.ToString()
									});
								}
							}
						}

						if (searchResultModel.TotalWeight > 0)
						{
							searchResult.Add(searchResultModel);
						}
					}
				}
			}
		}
		ApplyTransitiveWeights(data, searchResult);
		return searchResult;
	}

	private void ApplyTransitiveWeights(DataFile dataFile, List<SearchResultModel> searchResultModels)
	{
		var locks = dataFile.Locks;
		var locksSearchResult = searchResultModels.Where(x => x.EntityType == "Lock").ToList();
		var buildingSearchResult = searchResultModels.Where(x => x.EntityType == "Building").ToList();
		foreach (var buildingSearchModel in buildingSearchResult)
		{
			var buildingsLocks = locks.Where(l => l.BuildingId == buildingSearchModel.Id).ToList();
			foreach (var buildingsLock in buildingsLocks)
			{
				var lockSearchModel = locksSearchResult.FirstOrDefault(l => l.Id == buildingsLock.Id);
				if (lockSearchModel != null)
				{
					foreach (var buildingMatchedProperty in buildingSearchModel.MatchedPropertiesList)
					{
						if (Weights.GetModelWeightsDictionary("BuildingLock").ContainsKey(buildingMatchedProperty.PropertyName))
						{
							lockSearchModel.MatchedPropertiesList.Add(new MatchedProperties()
							{
								PropertyName = "Building_"+buildingMatchedProperty.PropertyName,
								CalculatedWeight = Weights.GetModelWeightsDictionary("BuildingLock")[buildingMatchedProperty.PropertyName],
								SearchMatchType = SearchMatchType.TransitiveMatch.ToString()
							});
						}
					}
				}
				else
				{
					lockSearchModel = new SearchResultModel()
					{
						Id = buildingsLock.Id,
						EntityType = buildingsLock.GetType().Name,
						Entity = JsonConvert.SerializeObject(buildingsLock),
						MatchedPropertiesList = new List<MatchedProperties>()
					};
					foreach (var buildingMatchedProperty in buildingSearchModel.MatchedPropertiesList)
					{
						if (Weights.GetModelWeightsDictionary("BuildingLock").ContainsKey(buildingMatchedProperty.PropertyName))
						{
							lockSearchModel.MatchedPropertiesList.Add(new MatchedProperties()
							{
								PropertyName = "Building_"+buildingMatchedProperty.PropertyName,
								CalculatedWeight = Weights.GetModelWeightsDictionary("BuildingLock")[buildingMatchedProperty.PropertyName],
								SearchMatchType = SearchMatchType.TransitiveMatch.ToString()
							});
						}
					}
					searchResultModels.Add(lockSearchModel);
				}
			}
		}
	}
}