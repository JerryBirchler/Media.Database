namespace Media.Database.Models;

public class ViewWordFiles
{
    public WordOrigin Origin { get; set; }
    public int WordId { get; set; }
    public string Word {  get; set; }
    public Guid FileId { get; set; }
    public bool? IsCurrent { get; set; }
    public bool? IsProperName { get; set; }
}
