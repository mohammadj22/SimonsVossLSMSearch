namespace LSMSearch.Models;

public class Lock
{
	public string Id { get; set; }
	public string BuildingId { get; set; }
	public LockType Type { get; set; }
	public string Name { get; set; }
	public string SerialNumber { get; set; }
	public string Floor { get; set; }
	public string RoomNumber { get; set; }
	public string Description { get; set; }
}