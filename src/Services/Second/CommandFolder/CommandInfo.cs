using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using ErrorOr;

namespace Cs2PracticeMode.Services.Second.CommandFolder;

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

    public ErrorOr<Success> GotArgsCountError(int requiredArgCount)
    {
        return GotArgsCount(requiredArgCount)
            ? Result.Success
            : Errors.Fail($"{requiredArgCount} are required but only {Args.Length} args are available");
    }

    public ErrorOr<string> GetArgString()
    {
        if (GotArgsCount(1) == false)
        {
            return Errors.Fail();
        }

        return Args.First();
    }

    public ErrorOr<int> GetArgInt()
    {
        if (GotArgsCount(1) == false)
        {
            return Errors.Fail();
        }

        var firstArg = Args.First();
        if (int.TryParse(firstArg, out var intArg) == false)
        {
            return Errors.Fail();
        }

        return intArg;
    }

    public ErrorOr<double> GetArgDouble()
    {
        var gotArgCountResult = GotArgsCountError(1);
        if (gotArgCountResult.IsError)
        {
            return gotArgCountResult.FirstError;
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