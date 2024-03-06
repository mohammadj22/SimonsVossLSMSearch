namespace LSMSearch.Models;

public class DataFile
{
	public List<Building> Buildings { get; set; } = new();
	public List<Lock> Locks { get; set; } = new();
	public List<Group> Groups { get; set; } = new();
	public List<Media> Media { get; set; } = new();
}