using Cs2PracticeMode.Constants;
using ErrorOr;

namespace Cs2PracticeMode.Storage.Collection;

public class LocalStorageCollection<T> : IStorageCollection<T> where T : IDataCollection
{
    private readonly string _dataDirectory;
    private readonly string _idFilePath;
    private readonly object _idLock = new();

    public LocalStorageCollection(string storageDirectory)
    {
        _idFilePath = Path.Combine(storageDirectory, "id");
        _dataDirectory = Path.Combine(storageDirectory, "data");

        if (Directory.Exists(storageDirectory) == false)
        {
            Directory.CreateDirectory(storageDirectory);
        }

        if (File.Exists(_idFilePath) == false)
        {
            File.WriteAllText(_idFilePath, "0");
        }

        if (Directory.Exists(_dataDirectory) == false)
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    public ErrorOr<List<T>> GetAll()
    {
        var result = new List<T>();
        var files = Directory.GetFiles(_dataDirectory);
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var dataObj = StorageHelper.Deserialize<T>(json);
            if (dataObj.IsError)
            {
                return Errors.Fail($"{dataObj.ErrorMessage()} \"{file}\"");
            }

            result.Add(dataObj.Value);
        }

        return result;
    }

    public ErrorOr<T> Get(uint id)
    {
        var filePath = GetDataFilePath(id);
        if (File.Exists(filePath) == false)
        {
            return Error.NotFound(description: $"File not found. \"{filePath}\"");
        }

        var json = File.ReadAllText(filePath);
        var data = StorageHelper.Deserialize<T>(json);
        if (data.IsError)
        {
            return Errors.Fail($"{data.ErrorMessage()} \"{filePath}\"");
        }

        return data.Value;
    }

    public ErrorOr<T> Add(T data)
    {
        if (File.Exists(_idFilePath) == false)
        {
            return Error.NotFound(description: $"Id file not found. \"{_idFilePath}\"");
        }

        lock (_idLock)
        {
            var currentIdString = File.ReadAllText(_idFilePath);
            if (uint.TryParse(currentIdString, out var currentId) == false)
            {
                return Errors.Fail($"Failed to parse current id. \"{currentIdString}\"");
            }

            var newId = currentId + 1;
            data.Id = newId;

            if (Directory.Exists(_dataDirectory) == false)
            {
                return Error.NotFound($"Data directory not found. \"{_dataDirectory}\"");
            }

            var filePath = GetDataFilePath(newId);
            if (File.Exists(filePath))
            {
                return Errors.Fail($"Data file already exists. \"{filePath}\"");
            }

            var json = StorageHelper.Serialize(data);
            if (json.IsError)
            {
                return json.FirstError;
            }

            File.WriteAllText(filePath, json.Value);

            // update the id after successful data file creation
            File.WriteAllText(_idFilePath, newId.ToString());
            return data;
        }
    }

    public ErrorOr<Success> Update(T data)
    {
        var filePath = GetDataFilePath(data.Id);
        if (File.Exists(filePath) == false)
        {
            return Errors.Fail($"File dose not exist. \"{filePath}\"");
        }

        data.UpdatedUtc = DateTime.UtcNow;

        var json = StorageHelper.Serialize(data);
        if (json.IsError)
        {
            return json.FirstError;
        }

        File.WriteAllText(filePath, json.Value);
        return Result.Success;
    }

    public ErrorOr<Deleted> Delete(uint id)
    {
        var filePath = GetDataFilePath(id);
        if (File.Exists(filePath) == false)
        {
            return Errors.Fail($"File dose not exist. \"{filePath}\"");
        }

        File.Delete(filePath);
        return Result.Deleted;
    }

    public bool Exist(uint id)
    {
        var filePath = GetDataFilePath(id);
        return File.Exists(filePath);
    }

    private string GetDataFilePath(uint id)
    {
        return Path.Combine(_dataDirectory, $"{id}.json");
    }
}