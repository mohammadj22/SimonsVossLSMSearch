namespace LSMSearch.Models;

public class DataFile
{
	public List<Building> Buildings { get; set; }
	public List<Lock> Locks { get; set; }
	public List<Group> Groups { get; set; }
	public List<Media> Media { get; set; }
}