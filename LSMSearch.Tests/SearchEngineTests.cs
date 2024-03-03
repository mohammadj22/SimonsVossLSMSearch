using FluentAssertions;
using LSMSearch.Models;
using Newtonsoft.Json;
using NSubstitute;

namespace LSMSearch.Tests;

public class SearchEngineTests
{
	private readonly IFetchLsmDataService _fetchLsmDataService = Substitute.For<IFetchLsmDataService>();
	
	[Fact]
	public async Task Search_ResultShouldContain1BuildingAnd10Locks_WhenCalled()
	{
		// arrange
		var data = JsonConvert.DeserializeObject<DataFile>(LsmTestsResources.Data_OneBuilding_10RelatedLocks);
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var input = data.Buildings.First().Name;
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		// act
		var result = await searchEngine.Search(input);
		
		// assert
		result.Count.Should().Be(11);
		result.Count(srm => srm.EntityType == nameof(Building)).Should().Be(1);
		result.Count(srm => srm.EntityType == nameof(Lock)).Should().Be(10);
	}

	[Fact]
	public async Task Search_BuildingShouldHaveExpectedWeight_WhenCalledWithWithFullMatchNameAndPartialMatchDescriptionAsInput()
	{
		// arrange
		var searchInput = "Head Office";
		var expectedWeight = 95;
		var data = JsonConvert.DeserializeObject<DataFile>(LsmTestsResources.Data_OneBuildingAndTwoRelatedLocks);
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		// act
		var result = await searchEngine.Search(searchInput);
		var buildingSearchResult = result.First(srm => srm.EntityType == nameof(Building));
		
		// assert
		buildingSearchResult.TotalWeight.Should().Be(expectedWeight);
		buildingSearchResult.MatchedPropertiesList.Count.Should().Be(2);
		buildingSearchResult.MatchedPropertiesList.Select(mp => mp.PropertyName).Should().Contain("Name");
		buildingSearchResult.MatchedPropertiesList.First(mp => mp.PropertyName == "Name").CalculatedWeight.Should().Be(90);
		buildingSearchResult.MatchedPropertiesList.Select(mp => mp.PropertyName).Should().Contain("Description");
		buildingSearchResult.MatchedPropertiesList.First(mp => mp.PropertyName == "Description").CalculatedWeight.Should().Be(5);
	}
	
	[Fact]
	public async Task Search_BuildingLocksShouldHaveExpectedWeight_WhenCalledWithWithFullMatchNameAndPartialMatchDescriptionAsInput()
	{
		// arrange
		var searchInput = "Head Office";
		var expectedWeight = 8;
		var data = JsonConvert.DeserializeObject<DataFile>(LsmTestsResources.Data_OneBuildingAndTwoRelatedLocks);
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		// act
		var result = await searchEngine.Search(searchInput);
		var locks = result.Where(srm => srm.EntityType == nameof(Lock)).ToList();
		
		// assert
		foreach (var @lock in locks)
		{
			@lock.TotalWeight.Should().Be(expectedWeight);
		}
	}
	
	[Theory]
	[InlineData(".OG", 16)] // floor: 6 + name: 10 these contain .OG
	[InlineData("UID-", 8)]
	public async Task Search_SearchResultShouldContain2LocksWithExpectedWeights_WhenCalledWithPartialMatchForLocks(string searchInput, int expectedWeight)
	{
		// arrange
		var data = JsonConvert.DeserializeObject<DataFile>(LsmTestsResources.Data_OneBuildingAndTwoRelatedLocks);
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		// act
		var result = await searchEngine.Search(searchInput);
		var locks = result.Where(srm => srm.EntityType == nameof(Lock)).ToList();
		
		// assert
		foreach (var @lock in locks)
		{
			@lock.TotalWeight.Should().Be(expectedWeight);
		}
	}
	
	[Fact]
	public async Task Search_ResultShouldContain1GroupAnd3Media_WhenCalled()
	{
		// arrange
		var data = JsonConvert.DeserializeObject<DataFile>(LsmTestsResources.Data_OneGroupThreeMedia);
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var input = "default";
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		// act
		var result = await searchEngine.Search(input);
		
		// assert
		result.Count.Should().Be(4);
		result.Count(srm => srm.EntityType == nameof(Group)).Should().Be(1);
		result.Count(srm => srm.EntityType == nameof(Media)).Should().Be(3);
	}

}