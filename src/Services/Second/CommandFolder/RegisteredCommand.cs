using System.Text;
using CounterStrikeSharp.API.Core;
using ErrorOr;

namespace Cs2PracticeMode.Services.Second.CommandFolder;

public delegate ErrorOr<Success> CommandAction(CCSPlayerController player, CommandInfo commandInfo);

public record RegisteredCommand(
    string Command,
    CommandAction CommandAction,
    ArgOption[] ArgOptions,
    string[] RequiredFlags)
{
    public List<string> GetHelp()
    {
        var result = new List<string>();
        foreach (var argOption in ArgOptions)
        {
            var sb = new StringBuilder();
            sb.Append($"{CoreConfig.PublicChatTrigger.First()}{Command}");
            foreach (var arg in argOption.Args.Where(a => a.Type != ArgType.None))
                sb.Append($" [{arg.Description}({arg.Type.ToString()})]");

            sb.Append($" // {argOption.Description}");
            result.Add(sb.ToString());
        }

        return result;
    }
}