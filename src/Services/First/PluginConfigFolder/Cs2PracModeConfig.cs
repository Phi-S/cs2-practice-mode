using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Cs2PracticeMode.Services.First.PluginConfigFolder;

public class Cs2PracModeConfig : BasePluginConfig
{
    [JsonPropertyName("ChatPrefix")]
    public string ChatPrefix { get; set; } = $"[{ChatColors.Red}Cs2PracticeMode]{ChatColors.Default} ";

    [JsonPropertyName("EnablePermissions")]
    public bool EnablePermissions { get; set; } = false;

    [JsonPropertyName("EnableFakeRcon")] public bool EnableFakeRcon { get; set; } = false;

    [JsonPropertyName("FakeRconPassword")] public string FakeRconPassword { get; set; } = "";
    [JsonPropertyName("DataLocation")] public string DataLocation { get; set; } = "";
}