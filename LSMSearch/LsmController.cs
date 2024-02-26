using Microsoft.AspNetCore.Mvc;

namespace LSMSearch;

[ApiController]
[Route("api/[controller]")]
public class LsmController : ControllerBase
{
	private readonly FetchLsmDataService _fetchLsmDataService;

	public LsmController(FetchLsmDataService fetchLsmDataService)
	{
		_fetchLsmDataService = fetchLsmDataService;
	}

	[HttpGet("search")]
	public async Task<IActionResult> Search([FromQuery] string input)
	{
		return Ok(await _fetchLsmDataService.ReadFromFileAndReturnData());
	}
}