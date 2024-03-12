using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._3.LastThrownGrenadeFolder;
using Cs2PracticeMode.Storage.Collection;
using ErrorOr;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Cs2PracticeMode.Services._1.GrenadeStorageFolder;

public class Vector3Json
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }

    /// <summary>
    /// Needed for Deserialization
    /// </summary>
    public Vector3Json()
    {
    }

    public Vector3Json(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3Json(Vector vector3) : this(vector3.X, vector3.Y, vector3.Z)
    {
    }

    public Vector3Json(QAngle vector3) : this(vector3.X, vector3.Y, vector3.Z)
    {
    }

    public Vector ToCsVector()
    {
        return new Vector(X, Y, Z);
    }

    public QAngle ToQAngle()
    {
        return new QAngle(X, Y, Z);
    }
}

public class GrenadeJsonModel : IDataCollection
{
    [JsonPropertyName("map")] public required string Map { get; init; }
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("type")]
    public required GrenadeType_t Type { get; init; }

    [JsonPropertyName("tags")] public required List<string> Tags { get; init; }
    [JsonPropertyName("playerSteamId")] public required ulong PlayerSteamId { get; init; }
    [JsonPropertyName("throwPosition")] public required Vector3Json ThrowPosition { get; init; }
    [JsonPropertyName("initialPosition")] public required Vector3Json InitialPosition { get; init; }
    [JsonPropertyName("angle")] public required Vector3Json Angle { get; init; }
    [JsonPropertyName("velocity")] public required Vector3Json Velocity { get; init; }

    [JsonPropertyName("detonationPosition")]
    public required Vector3Json DetonationPosition { get; init; }

    [JsonPropertyName("id")] public uint Id { get; set; }

    [JsonPropertyName("updatedUtc")] public required DateTime UpdatedUtc { get; set; }
    [JsonPropertyName("createdUtc")] public required DateTime CreatedUtc { get; set; }

    public static ErrorOr<GrenadeJsonModel> FromGrenade(Grenade grenade, string name, CCSPlayerController player)
    {
        if (grenade.DetonationPosition is null)
        {
            return Errors.Fail("Grenade is not yet detonated");
        }

        return new GrenadeJsonModel
        {
            Map = Server.MapName,
            Name = name,
            Description = "",
            Type = grenade.Type,
            Tags = new List<string>(),
            PlayerSteamId = player.SteamID,
            ThrowPosition = new Vector3Json(grenade.ThrowPosition),
            InitialPosition = new Vector3Json(grenade.InitialPosition),
            Angle = new Vector3Json(grenade.Angle),
            Velocity = new Vector3Json(grenade.Velocity),
            DetonationPosition = new Vector3Json(grenade.DetonationPosition),
            UpdatedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.UtcNow
        };
    }

    public ErrorOr<Success> RemoveTag(string tagToRemove)
    {
        tagToRemove = tagToRemove.ToLower().Trim();
        for (var i = Tags.Count - 1; i >= 0; i--)
        {
            var tag = Tags[i].ToLower().Trim();
            if (tag.Equals(tagToRemove))
            {
                Tags.RemoveAt(i);
                return Result.Success;
            }
        }

        return Errors.Fail($"Grenade is not tagged with \"{tagToRemove}\"");
    }
}