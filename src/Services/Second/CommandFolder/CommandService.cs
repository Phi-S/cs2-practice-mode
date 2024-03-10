using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.MessagingFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services.Second.CommandFolder;

public class CommandService : Base
{
    private readonly ConcurrentDictionary<string, string> _globalCommandAlias = new();
    private readonly MessagingService _messagingService;

    private readonly ConcurrentDictionary<CCSPlayerController, ConcurrentDictionary<string, string>>
        _playerCommandAliases = new();

    private readonly ConcurrentDictionary<string, RegisteredCommand> _registeredCommands = new();

    public CommandService(MessagingService messagingService) : base(LoadOrder.Second)
    {
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventPlayerChat>(EventHandlerOnPlayerChat);

        RegisterCommand(ChatCommands.Help,
            CommandActionHelp,
            ArgOption.NoArgs("Print all available commands"),
            ArgOption.OneString("Print help for command", "command"));

        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        _registeredCommands.Clear();
        _globalCommandAlias.Clear();
        _playerCommandAliases.Clear();

        base.Unload(plugin);
    }

    private ErrorOr<Success> CommandActionHelp(CCSPlayerController player, CommandInfo commandInfo)
    {
        var allCommand = _registeredCommands.Values.ToList();
        var arg = commandInfo.GetArgString();
        if (arg.IsError == false)
        {
            allCommand.RemoveAll(c => c.Command.Contains(arg.Value) == false);
        }

        foreach (var command in allCommand)
        foreach (var commandHelp in command.GetHelp())
            _messagingService.MsgToPlayerChat(player, commandHelp);

        return Result.Success;
    }

    public void RegisterCommand(
        string command,
        CommandAction commandAction,
        ArgOption argOptions,
        string requiredFlag)
    {
        RegisterCommand(
            command,
            commandAction,
            new[] { argOptions },
            new[] { requiredFlag }
        );
    }

    public void RegisterCommand(
        string command,
        CommandAction commandAction,
        ArgOption argOption1,
        ArgOption argOption2,
        string requiredFlag)
    {
        RegisterCommand(command, commandAction, new[] { argOption1, argOption2 }, new[] { requiredFlag });
    }

    public void RegisterCommand(
        string command,
        CommandAction commandAction,
        ArgOption argOption,
        string requiredFlag1,
        string requiredFlag2)
    {
        RegisterCommand(command, commandAction, new[] { argOption }, new[] { requiredFlag1, requiredFlag2 });
    }


    public void RegisterCommand(
        string command,
        CommandAction commandAction,
        ArgOption argOption1,
        ArgOption argOption2,
        string requiredFlag1,
        string requiredFlag2)
    {
        RegisterCommand(command, commandAction, new[] { argOption1, argOption2 },
            new[] { requiredFlag1, requiredFlag2 });
    }

    public void RegisterCommand(
        string command,
        CommandAction commandAction,
        params ArgOption[] argOptions)
    {
        RegisterCommand(command, commandAction, argOptions, null);
    }

    public void RegisterCommand(
        string command,
        CommandAction commandAction,
        ArgOption[] argOptions,
        string[]? requiredFlags)
    {
        command = command.ToLower().Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new RegisterCommandException("Cant register empty command");
        }

        var registeredCommand = new RegisteredCommand(
            command,
            commandAction,
            argOptions,
            requiredFlags ?? Array.Empty<string>());

        if (_registeredCommands.TryAdd(command, registeredCommand) == false)
        {
            throw new RegisterCommandException(command);
        }
    }

    public ErrorOr<Success> RegisterAlias(string alias, string command, CCSPlayerController? player = null)
    {
        alias = alias.ToLower().Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return Errors.Fail("Cant register empty alias");
        }

        command = command.ToLower().Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return Errors.Fail("Cant register an alias pointing to an empty command");
        }

        if (IsMenuCommands(alias, CoreConfig.PublicChatTrigger.First()))
        {
            return Errors.Fail($"Cant register alias. \"{command}\" is reserved for menus");
        }

        if (_registeredCommands.ContainsKey(alias))
        {
            return Errors.Fail("Command with the same name as the alias is already registered");
        }

        if (_globalCommandAlias.ContainsKey(alias))
        {
            return Errors.Fail("Global alias with the same name as the alias is already registered");
        }

        if (_registeredCommands.ContainsKey(command) == false)
        {
            return Errors.Fail("Alias target command is not registered");
        }

        if (player is null)
        {
            if (_globalCommandAlias.TryAdd(alias, command) == false)
            {
                return Errors.Fail("Global alias with the same name is already registered");
            }
        }
        else
        {
            if (_playerCommandAliases.TryGetValue(player, out var playerAliases))
            {
                if (playerAliases.TryAdd(alias, command) == false)
                {
                    return Errors.Fail("Another alias with the same name is already registered");
                }
            }
            else
            {
                var newPlayerAliases = new ConcurrentDictionary<string, string>();
                if (newPlayerAliases.TryAdd(alias, command) == false ||
                    _playerCommandAliases.TryAdd(player, newPlayerAliases) == false)
                {
                    return Errors.Fail("Failed to create new player aliases");
                }
            }
        }

        return Result.Success;
    }

    public ErrorOr<Success> DeregisterAlias(string alias, CCSPlayerController? player = null)
    {
        alias = alias.ToLower().Trim();
        if (player is null)
        {
            if (_globalCommandAlias.TryRemove(alias, out _) == false)
            {
                return Errors.Fail($"Alias \"{alias}\" dose not exist");
            }
        }
        else
        {
            if (_playerCommandAliases.TryGetValue(player, out var playerAliases) == false ||
                playerAliases.TryRemove(alias, out _) == false)
            {
                return Errors.Fail($"Alias \"{alias}\" dose not exist");
            }
        }

        return Result.Success;
    }

    public ErrorOr<string> GetAliasCommand(string alias, CCSPlayerController? player = null)
    {
        alias = alias.ToLower().Trim();
        if (player is null)
        {
            if (_globalCommandAlias.TryGetValue(alias, out var command))
            {
                return command;
            }
        }
        else
        {
            if (_playerCommandAliases.TryGetValue(player, out var playerAliases))
            {
                if (playerAliases.TryGetValue(alias, out var playerAliasCommand))
                {
                    return playerAliasCommand;
                }
            }
        }

        return Errors.Fail("Alias not registered");
    }

    private HookResult EventHandlerOnPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        var playerText = @event.Text;
        if (string.IsNullOrWhiteSpace(playerText))
        {
            return HookResult.Continue;
        }

        playerText = playerText.Trim();
        var triggerResult = GetTrigger(playerText);
        if (triggerResult.IsError)
        {
            return HookResult.Continue;
        }

        if (IsMenuCommands(playerText, triggerResult.Value.trigger))
        {
            return HookResult.Continue;
        }

        var (trigger, silentTrigger) = triggerResult.Value;
        playerText = playerText[trigger.Length..];

        var playerResult = GetPlayer(@event.Userid);
        if (playerResult.IsError)
        {
            return HookResult.Continue;
        }

        var player = playerResult.Value;

        var playerTextSplit = playerText.Split(" ");
        var command = playerTextSplit.First();
        var args = playerTextSplit.Skip(1).ToArray();

        var registeredCommandResult = GetRegisteredCommand(player, command);
        if (registeredCommandResult.IsError)
        {
            _messagingService.MsgToPlayerChat(player, "Command not recognised");
            return HookResult.Continue;
        }

        var registeredCommand = registeredCommandResult.Value;
        if (HasPermissionToExecuteCommand(player, registeredCommand) == false)
        {
            _messagingService.MsgToPlayerChat(player, "You are not authorized to use this command");
            return HookResult.Continue;
        }

        if (registeredCommand.ArgOptions.Any(o =>
                o.Args.Length == 1 && o.Args.First().Type == ArgType.OneLargeString) &&
            args.Length >= 1)
        {
            args = new[] { string.Join(" ", args) };
        }
        else
        {
            if (registeredCommand.ArgOptions.Any(o => o.Args.Count(a => a.Type != ArgType.None) == args.Length) ==
                false)
            {
                _messagingService.MsgToPlayerChat(player, "Command args not valid. Available options:");
                foreach (var helpText in registeredCommand.GetHelp())
                    _messagingService.MsgToPlayerChat(player, helpText);

                return HookResult.Continue;
            }
        }

        var commandInfo = new CommandInfo(registeredCommand, player, trigger, silentTrigger, args);
        var commandResult = registeredCommand.CommandAction.Invoke(player, commandInfo);
        if (commandResult.IsError)
        {
            if (commandResult.FirstError == Error.Validation())
            {
            }

            _messagingService.MsgToPlayerChat(player, commandResult.ErrorMessage());
        }

        return HookResult.Continue;
    }

    private static bool IsMenuCommands(string command, string trigger)
    {
        var match = Regex.Match(command, @$"(\{trigger}\d{{1}})");
        return match.Success;
    }

    private static bool HasPermissionToExecuteCommand(CCSPlayerController player, RegisteredCommand registeredCommand)
    {
        if (registeredCommand.RequiredFlags.Length == 0)
        {
            return true;
        }

        if (AdminManager.PlayerInGroup(player, Permissions.Groups.Root) ||
            AdminManager.PlayerHasPermissions(player, Permissions.Flags.Rcon))
        {
            return true;
        }

        var requiredGroups = registeredCommand.RequiredFlags.Where(f => f.StartsWith("#")).ToArray();
        foreach (var requiredGroup in requiredGroups)
            if (AdminManager.PlayerInGroup(player, requiredGroup))
            {
                return true;
            }

        var requiredFlags = registeredCommand.RequiredFlags.Where(f => f.StartsWith("@")).ToArray();
        foreach (var requiredFlag in requiredFlags)
            if (AdminManager.PlayerHasPermissions(player, requiredFlag))
            {
                return true;
            }

        return false;
    }


    #region Trigger

    private ErrorOr<(string trigger, bool silentTrigger)> GetTrigger(string playerText)
    {
        var publicChatTrigger = CoreConfig.PublicChatTrigger;
        var usedTrigger = publicChatTrigger.FirstOrDefault(playerText.StartsWith);
        if (usedTrigger is not null)
        {
            return (usedTrigger, false);
        }

        var silentChatTrigger = CoreConfig.SilentChatTrigger;
        usedTrigger = silentChatTrigger.FirstOrDefault(playerText.StartsWith);
        if (usedTrigger is not null)
        {
            return (usedTrigger, true);
        }

        return Error.Failure();
    }

    #endregion

    #region GetPlayer

    private ErrorOr<CCSPlayerController> GetPlayer(int userId)
    {
        var allPlayers = Utilities.GetPlayers();
        foreach (var player in allPlayers)
            if (player.UserId == userId)
            {
                return player;
            }

        return Errors.PlayerNullOrNotValid();
    }

    #endregion

    #region GetArgs

    public ErrorOr<List<string>> GetArgs(string playerText)
    {
        var playerTextSplit = playerText.Split(" ");
        if (playerText.Length == 1)
        {
            return Errors.Fail("No args provided");
        }

        return playerTextSplit.Skip(1).ToList();
    }

    #endregion

    #region RegisteredCommand

    private ErrorOr<RegisteredCommand> GetRegisteredCommand(CCSPlayerController player, string command)
    {
        if (_registeredCommands.TryGetValue(command, out var registeredCommand))
        {
            return registeredCommand;
        }

        if (_globalCommandAlias.TryGetValue(command, out var aliasCommand))
        {
            if (_registeredCommands.TryGetValue(aliasCommand, out registeredCommand))
            {
                return registeredCommand;
            }
        }

        if (_playerCommandAliases.TryGetValue(player, out var playerCommandAlias))
        {
            if (playerCommandAlias.TryGetValue(command, out aliasCommand))
            {
                if (_registeredCommands.TryGetValue(aliasCommand, out registeredCommand))
                {
                    return registeredCommand;
                }
            }
        }

        return Error.NotFound();
    }

    #endregion
}