namespace Cs2PracticeMode.Storage.Collection;

public interface IDataCollection
{
    public uint Id { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}