using AutoFixture;
using AutoFixture.Xunit2;
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
	public async Task
		Search_BuildingShouldHaveExpectedWeight_WhenCalledWithWithFullMatchNameAndPartialMatchDescriptionAsInput()
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
		buildingSearchResult.MatchedPropertiesList.First(mp => mp.PropertyName == "Description").CalculatedWeight.Should()
			.Be(5);
	}

	[Fact]
	public async Task
		Search_BuildingLocksShouldHaveExpectedWeight_WhenCalledWithWithFullMatchNameAndPartialMatchDescriptionAsInput()
	{
		// arrange
		var searchInput = "Head Office";
		var expectedWeight = 80;
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
	public async Task Search_SearchResultShouldContain2LocksWithExpectedWeights_WhenCalledWithPartialMatchForLocks(
		string searchInput, int expectedWeight)
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

	[Theory, AutoData]
	public async Task Search_MediaShouldHaveValidTransitiveWeight_WhenCalled(Media media)
	{
		// arrange
		
		// full match for name x10
		var searchInput = "Main Entrance";
		var group = new Group()
		{
			Id = Guid.NewGuid().ToString(),
			Name = searchInput, // full match 9 * 10
			Description = searchInput + Guid.NewGuid().ToString()[..8] // add +5 weight, partial match
		};
		media.GroupId = group.Id;
		var data = new DataFile
		{
			Groups = new List<Group>() { group },
			Media = new List<Media>() { media }
		};
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		// act
		var result = await searchEngine.Search(searchInput);
		var groupSearchResultModel = result.First(srm => srm.EntityType == nameof(Group));
		var mediaSearchResultModel = result.First(srm => srm.EntityType == nameof(Media));
		
		// assert
		groupSearchResultModel.TotalWeight.Should().Be(95);
		mediaSearchResultModel.TotalWeight.Should().Be(8 * 10); // full match 
	}

	[Theory, AutoData]
	public async Task Search_ShouldSearchRelatedEntityId_WhenCalled(Media media)
	{
		// arrange
		var groupId = Guid.NewGuid().ToString();
		var searchInput = groupId[..6];
		var group = new Group()
		{
			Id = groupId,
			Name = Guid.NewGuid().ToString()[..8],
			Description = Guid.NewGuid().ToString()[..8]
		};
		media.GroupId = group.Id;
		var data = new DataFile
		{
			Groups = new List<Group>() { group },
			Media = new List<Media>() { media }
		};
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		var result = await searchEngine.Search(searchInput);
		var mediaSearchResultModelList = result.Where(srm => srm.EntityType == nameof(Media));
		
		// assert
		mediaSearchResultModelList.Count().Should().Be(0);
	}
	
	[Theory]
	[InlineData(MediaType.Card)]
	[InlineData(MediaType.Transponder)]
	[InlineData(MediaType.TransponderWithCardInlay)]
	public async Task Search_ShouldReturnCorrectMediaTypes_WhenSearchedByMediaType(MediaType expectedMediaType)
	{
		// Arrange
		var fixture = new Fixture();
		var group = fixture.Create<Group>();
		var data = new DataFile
		{
			Groups = new List<Group> { group },
			Media = new List<Media>
			{
				new Media
				{
					Id = Guid.NewGuid().ToString(),
					GroupId = group.Id,
					Type = expectedMediaType,
					Owner = "Owner Name",
					SerialNumber = "Serial",
					Description = "Description"
				},
			}
		};

		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		var searchInput = expectedMediaType.ToString();

		// Act
		var result = await searchEngine.Search(searchInput);

		// Assert
		result.Should().ContainSingle().Which.EntityType.Should().Be(nameof(Media));
		var searchResult = result.First();
		JsonConvert.DeserializeObject<Media>(searchResult.Entity).Type.Should().Be(expectedMediaType);
	}
	
	[Theory]
	[InlineData(LockType.Cylinder)]
	[InlineData(LockType.SmartHandle)]
	public async Task Search_ShouldReturnCorrectLockTypes_WhenSearchedByLockType(LockType expectedLockType)
	{
		// Arrange
		var fixture = new Fixture();
		var lockItem = fixture.Create<Lock>();
		var building = fixture.Create<Building>();
		var data = new DataFile
		{
			Buildings = new List<Building> { building },
			Locks = new List<Lock>
			{
				new Lock
				{
					Id = Guid.NewGuid().ToString(),
					BuildingId = building.Id,
					Type = expectedLockType,
					Name = "Lock Name",
					SerialNumber = "Serial",
					Floor = "Floor",
					RoomNumber = "Room Number",
					Description = "Description"
				},
			}
		};

		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		var searchInput = expectedLockType.ToString();

		// Act
		var result = await searchEngine.Search(searchInput);

		// Assert
		result.Should().ContainSingle().Which.EntityType.Should().Be(nameof(Lock));
		var searchResult = result.First();
		JsonConvert.DeserializeObject<Lock>(searchResult.Entity).Type.Should().Be(expectedLockType);
	}

	[Theory]
	[InlineData("homE Office")]
	[InlineData("HOME OFFICE")]
	[InlineData("home office")]
	public async Task Search_ShouldNotBeCaseSensitive_WhenCalled(string searchInput)
	{
		// arrange
		var name = "Home Office";
		var group = new Group()
		{
			Id = Guid.NewGuid().ToString(),
			Name = name
		};
		var data = new DataFile()
		{
			Groups = new List<Group>() { group }
		};
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		// act
		var result = await searchEngine.Search(searchInput);
		
		// assert
		result.Count().Should().Be(1);
	}

	[Theory, AutoData]
	public async Task Search_ShouldReturnCorrectResult_WhenSearchInputIsInBothBuildingAndItsRelatedLock(Building building,
		Lock @lock)
	{
		// act
		@lock.Name = "Head";
		@lock.BuildingId = building.Id;
		building.Name = "Head";
		var dataFile = new DataFile();
		dataFile.Buildings.Add(building);
		dataFile.Locks.Add(@lock);
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(dataFile);
		var searchEngine = new SearchEngine(_fetchLsmDataService);
		
		// act
		var result = await searchEngine.Search("Head");
		var buildingResult = result.First(srm => srm.EntityType == nameof(Building));
		var lockResult = result.First(srm => srm.EntityType == nameof(Lock));
		
		// assert
		buildingResult.TotalWeight.Should().Be(90); // full match
		lockResult.TotalWeight.Should().Be(80 + 100); // full match + transitive
	}
	
	// should be sorted
	[Fact]
	public async Task Search_ResultsShouldBeCorrectlySortedBasedOnRelevance()
	{
		// Arrange
		var data = PrepareTestFixture();
		_fetchLsmDataService.ReadFromFileAndReturnData().Returns(data);
		var searchEngine = new SearchEngine(_fetchLsmDataService);

		// Example search input that partially matches across entities
		var searchInput = data.Buildings.Last().Name.Substring(0, 3); // partial match input

		// Act
		var result = await searchEngine.Search(searchInput);

		// Assert
		// Verify that the results are sorted by total weight in descending order
		bool isSorted = true;
		for(int i = 0; i < result.Count - 1; i++)
		{
			if(result[i].TotalWeight < result[i + 1].TotalWeight)
			{
				isSorted = false;
				break;
			}
		}

		isSorted.Should().BeTrue();
	}
	
	private DataFile PrepareTestFixture()
	{
		var fixture = new Fixture();
		var buildings = fixture.CreateMany<Building>(2).ToList();
		var locks = new List<Lock>();
		var groups = fixture.CreateMany<Group>(2).ToList();
		var media = new List<Media>();

		// Assign some locks to the first building
		locks.AddRange(fixture.Build<Lock>()
			.With(l => l.BuildingId, buildings[0].Id)
			.CreateMany(5));
    
		// Assign some media to the first group
		media.AddRange(fixture.Build<Media>()
			.With(m => m.GroupId, groups[0].Id)
			.CreateMany(5));

		return new DataFile
		{
			Buildings = buildings,
			Locks = locks,
			Groups = groups,
			Media = media
		};
	}
}