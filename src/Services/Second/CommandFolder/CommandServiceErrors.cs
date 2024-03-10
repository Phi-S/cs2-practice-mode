using ErrorOr;

namespace Cs2PracticeMode.Services.Second.CommandFolder;

public static class CommandServiceErrors
{
    public static Error ArgError(string message)
    {
        return Error.Validation("CommandService.ArgValidation", message);
    }
}