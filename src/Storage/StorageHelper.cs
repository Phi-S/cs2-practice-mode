using System.Text.Json;
using Cs2PracticeMode.Constants;
using ErrorOr;

namespace Cs2PracticeMode.Storage;

public static class StorageHelper
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static ErrorOr<string> Serialize(object date)
    {
        var json = JsonSerializer.Serialize(date, JsonSerializerOptions);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Errors.Fail("Failed to serialize data");
        }

        return json;
    }

    public static ErrorOr<T> Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Errors.Fail("Can not deserialize empty json");
        }

        var data = JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
        if (data is null)
        {
            return Errors.Fail("Failed to deserialize json");
        }

        return data;
    }
}