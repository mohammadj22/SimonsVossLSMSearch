namespace LSMSearch.Models;

public class Media
{
	public string Id { get; set; }
	public string GroupId { get; set; }
	public MediaType Type { get; set; }
	public string Owner { get; set; }
	public string SerialNumber { get; set; }
	public string Description { get; set; }
}