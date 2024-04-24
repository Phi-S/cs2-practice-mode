using Cs2PracticeMode.SharedModels;

namespace Cs2PracticeMode.Services._3.SpawnFolder;

public record MapSpawns(string Map, List<Position> TerroristSpawns, List<Position> CounterTerroristSpawns);