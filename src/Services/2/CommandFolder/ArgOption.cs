namespace Cs2PracticeMode.Services._2.CommandFolder;

public enum ArgType
{
    None,
    Any,
    String,
    UInt,
    Double
}

public record ArgOption(string Description, params (ArgType Type, string ArgDescription)[] Args)
{
    public static ArgOption NoArgs(string commandDescription)
    {
        return new ArgOption(commandDescription, (ArgType.None, ""));
    }

    public static ArgOption Any(string commandDescription, string argDescription)
    {
        return new ArgOption(commandDescription, (ArgType.Any, argDescription));
    }

    public static ArgOption String(string commandDescription, string argDescription)
    {
        return new ArgOption(commandDescription, (ArgType.String, argDescription));
    }

    public static ArgOption UInt(string commandDescription, string argDescription)
    {
        return new ArgOption(commandDescription, (ArgType.UInt, argDescription));
    }

    public static ArgOption Double(string commandDescription, string argDescription)
    {
        return new ArgOption(commandDescription, (ArgType.Double, argDescription));
    }

    public static ArgOption StringString(string commandDescription, string firstArgDescription,
        string secondArgDescription)
    {
        return new ArgOption(commandDescription, (ArgType.String, firstArgDescription),
            (ArgType.String, secondArgDescription));
    }
}