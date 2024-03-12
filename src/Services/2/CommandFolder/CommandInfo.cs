using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using ErrorOr;

namespace Cs2PracticeMode.Services._2.CommandFolder;

public record CommandInfo(
    RegisteredCommand Command,
    CCSPlayerController Player,
    string Trigger,
    bool SilentTrigger,
    string[] Args)
{
    public int ArgCount => Args.Length;

    public bool GotArgsCount(int requiredParameterCount)
    {
        return Args.Length == requiredParameterCount;
    }

    public ErrorOr<string> GetArgString()
    {
        if (GotArgsCount(1) == false)
        {
            return Errors.Fail();
        }

        return Args.First();
    }

    public ErrorOr<uint> GetArgUInt()
    {
        if (GotArgsCount(1) == false)
        {
            return Errors.Fail();
        }

        var firstArg = Args.First();
        if (uint.TryParse(firstArg, out var intArg) == false)
        {
            return Errors.Fail();
        }

        return intArg;
    }

    public ErrorOr<double> GetArgDouble()
    {
        if (GotArgsCount(1) == false)
        {
            return Errors.Fail();
        }

        var firstArg = Args.First();
        if (double.TryParse(firstArg, out var doubleArg) == false)
        {
            return Errors.Fail($"Arg \"{firstArg}\" is not a valid number");
        }

        return doubleArg;
    }

    public ErrorOr<(string, string)> GetArgStringString()
    {
        if (GotArgsCount(2) == false)
        {
            return Errors.Fail();
        }

        return (Args.First(), Args.Last());
    }
}