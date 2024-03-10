namespace Cs2PracticeMode.Services.Second.CommandFolder;

public enum ArgType
{
    None,
    OneLargeString,
    String,
    Int,
    Double
}

public record ArgOption(string Description, params (ArgType Type, string Description)[] Args)
{
    public static ArgOption NoArgs(string commandDescription)
    {
        return new ArgOption(commandDescription, (ArgType.None, ""));
    }

    public static ArgOption OneLargeString(string commandDescription, string argDescription)
    {
        return new ArgOption(commandDescription, (ArgType.OneLargeString, argDescription));
    }

    public static ArgOption OneString(string commandDescription, string argDescription)
    {
        return new ArgOption(commandDescription, (ArgType.String, argDescription));
    }

    public static ArgOption SingleInt(string commandDescription, string argDescription)
    {
        return new ArgOption(commandDescription, (ArgType.Int, argDescription));
    }

    public static ArgOption SingleDouble(string commandDescription, string argDescription)
    {
        return new ArgOption(commandDescription, (ArgType.Double, argDescription));
    }

    public static ArgOption TwoStrings(string commandDescription, string firstArgDescription,
        string secondArgDescription)
    {
        return new ArgOption(commandDescription, (ArgType.String, firstArgDescription),
            (ArgType.String, secondArgDescription));
    }
}