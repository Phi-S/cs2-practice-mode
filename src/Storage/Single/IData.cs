namespace Cs2PracticeMode.Storage.Single;

public interface IData
{
    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}