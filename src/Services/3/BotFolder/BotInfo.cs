using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.SharedModels;

namespace Cs2PracticeMode.Services._3.BotFolder;

public record BotInfo(
    CCSPlayerController Controller,
    Position Position,
    CCSPlayerController Owner,
    bool Crouch,
    DateTime AddedUtc
);