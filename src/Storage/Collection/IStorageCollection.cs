using ErrorOr;

namespace Cs2PracticeMode.Storage.Collection;

public interface IStorageCollection<T> where T : IDataCollection
{
    public ErrorOr<List<T>> GetAll();
    public ErrorOr<T> Get(uint id);
    public ErrorOr<T> Add(T data);
    public ErrorOr<Success> Update(T data);
    public ErrorOr<Deleted> Delete(uint id);
    public bool Exist(uint id);
}