using ErrorOr;

namespace Cs2PracticeMode.Storage.Single;

public interface IStorageSingle<T> where T : IData
{
    public ErrorOr<T> Get();
    public ErrorOr<Success> AddOrUpdate(T data);
    public ErrorOr<Deleted> Delete();

    public bool Exists();
}