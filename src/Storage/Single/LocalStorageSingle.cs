using ErrorOr;

namespace Cs2PracticeMode.Storage.Single;

public class LocalStorageSingle<T> : IStorageSingle<T> where T : IData, new()
{
    private readonly object _fileLock = new();
    private readonly string _storageFilePath;

    public LocalStorageSingle(string storageDirectory, string storageName)
    {
        if (Directory.Exists(storageDirectory) == false)
        {
            Directory.CreateDirectory(storageDirectory);
        }

        _storageFilePath = Path.Combine(storageDirectory, storageName);
    }

    public ErrorOr<T> Get()
    {
        if (File.Exists(_storageFilePath) == false)
        {
            return Error.NotFound(description: $"File not found. \"{_storageFilePath}\"");
        }

        lock (_fileLock)
        {
            var json = File.ReadAllText(_storageFilePath);
            var data = StorageHelper.Deserialize<T>(json);
            if (data.IsError)
            {
                return data.FirstError;
            }

            return data.Value;
        }
    }

    public ErrorOr<Success> AddOrUpdate(T data)
    {
        lock (_fileLock)
        {
            if (File.Exists(_storageFilePath))
            {
                var currentFileJson = File.ReadAllText(_storageFilePath);
                var currentData = StorageHelper.Deserialize<T>(currentFileJson);
                if (currentData.IsError)
                {
                    return currentData.FirstError;
                }

                data.CreatedUtc = currentData.Value.CreatedUtc;
                data.UpdatedUtc = DateTime.UtcNow;

                var updatedJson = StorageHelper.Serialize(data);
                if (updatedJson.IsError)
                {
                    return updatedJson.FirstError;
                }

                File.WriteAllText(_storageFilePath, updatedJson.Value);
            }
            else
            {
                data.CreatedUtc = DateTime.UtcNow;
                data.UpdatedUtc = DateTime.UtcNow;

                var json = StorageHelper.Serialize(data);
                if (json.IsError)
                {
                    return json.FirstError;
                }

                File.WriteAllText(_storageFilePath, json.Value);
            }

            return Result.Success;
        }
    }

    public ErrorOr<Deleted> Delete()
    {
        throw new NotImplementedException();
    }

    public bool Exists()
    {
        return File.Exists(_storageFilePath);
    }
}