using Microsoft.AspNetCore.Mvc;

namespace LSMSearch;

[ApiController]
[Route("api/[controller]")]
public class LsmController : ControllerBase
{
	private readonly ISearchEngine _searchEngine;

	public LsmController(ISearchEngine searchEngine)
	{
		_searchEngine = searchEngine;
	}

	[HttpGet("search")]
	public async Task<IActionResult> Search([FromQuery] string input)
	{
		return Ok(await _searchEngine.Search(input));
	}
}