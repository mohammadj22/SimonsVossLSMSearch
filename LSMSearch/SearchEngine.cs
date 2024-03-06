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
						var searchResultModel = CreateSearchResultModel(entity, input, modelWeightsDictionary);
						if (searchResultModel.TotalWeight > 0)
						{
							searchResult.Add(searchResultModel);
						}
					}
				}
			}
		}

		ApplyTransitiveWeights(data, searchResult);
		searchResult.Sort((a, b) => b.TotalWeight.CompareTo(a.TotalWeight));
		return searchResult;
	}

	private SearchResultModel CreateSearchResultModel(IEntity entity, string input,
		Dictionary<string, int> modelWeightsDictionary)
	{
		var matchedProperties = new List<MatchedProperties>();
		foreach (var property in entity.GetType().GetProperties())
		{
			var propertyValue = property.GetValue(entity)?.ToString();
			if (propertyValue != null && !property.Name.EndsWith("Id"))
			{
				var isFullMatch = string.Equals(propertyValue, input, StringComparison.OrdinalIgnoreCase);
				var containsInput = propertyValue.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0;

				if (isFullMatch || containsInput)
				{
					matchedProperties.Add(new MatchedProperties
					{
						PropertyName = property.Name,
						CalculatedWeight = isFullMatch
							? Weights.FullMatchCoefficient * modelWeightsDictionary[property.Name]
							: modelWeightsDictionary[property.Name],
						SearchMatchType =
							isFullMatch ? SearchMatchType.FullMatch.ToString() : SearchMatchType.PartialMatch.ToString(),
					});
				}
			}
		}

		return new SearchResultModel
		{
			Id = entity.Id,
			EntityType = entity.GetType().Name,
			MatchedPropertiesList = matchedProperties,
			Entity = JsonConvert.SerializeObject(entity)
		};
	}

	private void ApplyTransitiveWeights(DataFile dataFile, List<SearchResultModel> searchResultModels)
	{
		ApplyTransitiveWeightsGeneric<Building, Lock>(dataFile.Locks, searchResultModels, "BuildingLock");
		ApplyTransitiveWeightsGeneric<Group, Media>(dataFile.Media, searchResultModels, "GroupMedia");
	}

	private void ApplyTransitiveWeightsGeneric<TParent, TChild>(
		IEnumerable<TChild> children, List<SearchResultModel> searchResultModels, string weightDictionaryKey)
		where TParent : IEntity
		where TChild : IEntity
	{
		var parentSearchResult = searchResultModels.Where(x => x.EntityType == typeof(TParent).Name).ToList();
		var childSearchResult = searchResultModels.Where(x => x.EntityType == typeof(TChild).Name).ToList();

		foreach (var parentSearchModel in parentSearchResult)
		{
			var parentIdProperty = typeof(TChild).GetProperty(typeof(TParent).Name + "Id");
			var parentChildren = children?.Where(c => parentIdProperty.GetValue(c).ToString() == parentSearchModel.Id);

			if (parentChildren == null) continue;
			foreach (var child in parentChildren)
			{
				var childSearchModel = childSearchResult.FirstOrDefault(m => m.Id == child.Id) ?? CreateSearchModel(child);

				ApplyTransitivePropertyWeights(parentSearchModel, childSearchModel, weightDictionaryKey);

				// Only add the child search model to the results if it was newly created
				if (childSearchModel.MatchedPropertiesList.Any() && !searchResultModels.Contains(childSearchModel))
				{
					searchResultModels.Add(childSearchModel);
				}
			}
		}
	}

	private SearchResultModel CreateSearchModel<T>(T entity) where T : IEntity
	{
		return new SearchResultModel
		{
			Id = entity.Id,
			EntityType = entity.GetType().Name,
			Entity = JsonConvert.SerializeObject(entity),
			MatchedPropertiesList = new List<MatchedProperties>()
		};
	}

	private void ApplyTransitivePropertyWeights(SearchResultModel sourceModel, SearchResultModel targetModel,
		string weightDictionaryKey)
	{
		var transitiveWeightsDictionary = Weights.GetModelWeightsDictionary(weightDictionaryKey);

		foreach (var sourceMatchedProperty in sourceModel.MatchedPropertiesList)
		{
			if (!transitiveWeightsDictionary.TryGetValue(sourceMatchedProperty.PropertyName, out var value)) continue;
			
			var weightCoefficient = sourceMatchedProperty.SearchMatchType == SearchMatchType.FullMatch.ToString() ? 10 : 1;
			targetModel.MatchedPropertiesList.Add(new MatchedProperties
			{
				PropertyName = sourceModel.EntityType + "_" + sourceMatchedProperty.PropertyName,
				CalculatedWeight = value * weightCoefficient,
				SearchMatchType = SearchMatchType.TransitiveMatch.ToString()
			});
		}
	}
}