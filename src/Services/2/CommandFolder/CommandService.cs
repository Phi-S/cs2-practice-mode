using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._1.AliasStorageFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services._2.CommandFolder;

public class CommandService : Base
{
    private readonly MessagingService _messagingService;
    private readonly AliasStorageService _aliasStorageService;

    private readonly ConcurrentDictionary<string, RegisteredCommand> _registeredCommands = new();

    public CommandService(MessagingService messagingService, AliasStorageService aliasStorageService) : base(
        LoadOrder.AboveNormal)
    {
        _messagingService = messagingService;
        _aliasStorageService = aliasStorageService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventPlayerChat>(EventHandlerOnPlayerChat);

        RegisterCommand(ChatCommands.Help,
            CommandActionHelp,
            new[]
            {
                ArgOption.NoArgs("Print all available commands"),
                ArgOption.String("Print help for command", "command")
            });

        RegisterCommand(ChatCommands.Alias,
            CommandActionPlayerAlias,
            new[]
            {
                ArgOption.String("Displays the command behind the alias", "alias"),
                ArgOption.StringString("Creates new player alias", "alias", "command")
            },
            new[] { Permissions.Flags.Alias });

        RegisterCommand(ChatCommands.RemoveAlias,
            CommandActionDeregisterPlayerAlias,
            ArgOption.String("Removes player alias", "alias"),
            Permissions.Flags.RemoveAlias);

        RegisterCommand(ChatCommands.GlobalAlias,
            CommandActionGlobalAlias,
            new[]
            {
                ArgOption.String("Displays the command behind the global alias", "alias"),
                ArgOption.StringString("Creates new global alias", "alias", "command")
            },
            new[] { Permissions.Flags.GlobalAlias });

        RegisterCommand(ChatCommands.RemoveGlobalAlias,
            CommandActionDeregisterGlobalAlias,
            ArgOption.String("Removes global alias", "alias"),
            Permissions.Flags.RemoveGlobalAlias);

        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        _registeredCommands.Clear();
        base.Unload(plugin);
    }

    #region CommandActions

    private ErrorOr<Success> CommandActionHelp(CCSPlayerController player, CommandInfo commandInfo)
    {
        var allCommand = _registeredCommands.Values.ToList();
        var arg = commandInfo.GetArgString();
        if (arg.IsError == false)
        {
            allCommand.RemoveAll(c => c.Command.Contains(arg.Value) == false);
        }

        foreach (var command in allCommand)
        {
            foreach (var commandHelp in command.GetHelp())
            {
                _messagingService.MsgToPlayerChat(player, commandHelp);
            }
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandActionPlayerAlias(CCSPlayerController player, CommandInfo commandInfo)
    {
        string alias;
        if (commandInfo.GotArgsCount(1))
        {
            var arg = commandInfo.GetArgString();
            if (arg.IsError)
            {
                return arg.FirstError;
            }

            alias = arg.Value;
            var commandForPlayerAlias = _aliasStorageService.GetCommandForPlayerAlias(player, alias);
            if (commandForPlayerAlias.IsError)
            {
                return commandForPlayerAlias.FirstError;
            }

            _messagingService.MsgToPlayerChat(player,
                $"The alias \"{alias}\" is pointing to the command \"{commandForPlayerAlias.Value}\"");
            return Result.Success;
        }


        var stringStringArg = commandInfo.GetArgStringString();
        if (stringStringArg.IsError)
        {
            return stringStringArg.FirstError;
        }

        alias = stringStringArg.Value.Item1;
        var command = stringStringArg.Value.Item2;
        var registerPlayerAliasResult = RegisterPlayerAlias(alias, command, player);
        if (registerPlayerAliasResult.IsError)
        {
            return registerPlayerAliasResult.FirstError;
        }

        _messagingService.MsgToPlayerChat(player,
            $"Player alias \"{alias}\" with the command \"{command}\" registered");
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionGlobalAlias(CCSPlayerController player, CommandInfo commandInfo)
    {
        string alias;
        if (commandInfo.GotArgsCount(1))
        {
            var arg = commandInfo.GetArgString();
            if (arg.IsError)
            {
                return arg.FirstError;
            }

            alias = arg.Value.ToLower().Trim();

            var commandForGlobalAlias = _aliasStorageService.GetCommandForGlobalAlias(alias);
            if (commandForGlobalAlias.IsError)
            {
                return commandForGlobalAlias.FirstError;
            }

            _messagingService.MsgToPlayerChat(player,
                $"The alias \"{alias}\" is pointing to the command \"{commandForGlobalAlias.Value}\"");
            return Result.Success;
        }


        var stringStringArg = commandInfo.GetArgStringString();
        if (stringStringArg.IsError)
        {
            return stringStringArg.FirstError;
        }

        alias = stringStringArg.Value.Item1;
        var command = stringStringArg.Value.Item2;
        var registerAliasResult = RegisterGlobalAlias(alias, command);
        if (registerAliasResult.IsError)
        {
            return registerAliasResult.FirstError;
        }

        _messagingService.MsgToPlayerChat(player,
            $"Global alias \"{alias}\" with the command \"{command}\" registered");
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionDeregisterGlobalAlias(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var alias = arg.Value.ToLower().Trim();
        var deleteGlobalAlias = _aliasStorageService.DeleteGlobalAlias(alias);
        if (deleteGlobalAlias.IsError)
        {
            return deleteGlobalAlias.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Global alias \"{alias}\" removed");
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionDeregisterPlayerAlias(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var alias = arg.Value.ToLower().Trim();
        var deletePlayerAlias = _aliasStorageService.DeletePlayerAlias(player, alias);
        if (deletePlayerAlias.IsError)
        {
            return deletePlayerAlias.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Personal alias \"{alias}\" removed");
        return Result.Success;
    }

    #endregion

    #region RegisterCommand

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
        ArgOption[] argOptions,
        string[]? requiredFlags = null)
    {
        RegisterCommandInternal(
            command,
            Array.Empty<string>(),
            commandAction,
            argOptions,
            requiredFlags ?? Array.Empty<string>());
    }

    private void RegisterCommandInternal(
        string command,
        string[] aliases,
        CommandAction commandAction,
        ArgOption[] argOptions,
        string[] requiredFlags)
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
            requiredFlags);

        if (_registeredCommands.TryAdd(command, registeredCommand) == false)
        {
            throw new RegisterCommandException(command);
        }

        foreach (var alias in aliases)
        {
            var commandForGlobalAlias = _aliasStorageService.GetCommandForGlobalAlias(alias);
            if (commandForGlobalAlias.IsError == false)
            {
                if (commandForGlobalAlias.Value == command)
                {
                    continue;
                }

                throw new Exception(
                    $"Failed to add alias \"{alias}\" for command \"{command}\". Another global alias already exists but is pointing to a different command");
            }

            var registerGlobalAlias = RegisterGlobalAlias(alias, command);
            if (registerGlobalAlias.IsError)
            {
                throw new Exception(
                    $"Failed to add global alias \"{alias}\" for command \"{command}\". {registerGlobalAlias.ErrorMessage()}");
            }
        }
    }

    #endregion

    private ErrorOr<Success> CanAddAlias(string alias, string command)
    {
        if (IsMenuCommands(alias, CoreConfig.PublicChatTrigger.First()))
        {
            return Errors.Fail($"Cant register alias. \"{command}\" is reserved for menus");
        }

        if (_registeredCommands.ContainsKey(alias))
        {
            return Errors.Fail("Command with the same name as the alias is already registered");
        }

        return Result.Success;
    }

    private ErrorOr<Success> RegisterGlobalAlias(string alias, string command)
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

        var canAddAlias = CanAddAlias(alias, command);
        if (canAddAlias.IsError)
        {
            return canAddAlias.FirstError;
        }

        var addGlobalAlias = _aliasStorageService.AddGlobalAlias(alias, command);
        if (addGlobalAlias.IsError)
        {
            return addGlobalAlias.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> RegisterPlayerAlias(string alias, string command, CCSPlayerController player)
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

        var canAddAlias = CanAddAlias(alias, command);
        if (canAddAlias.IsError)
        {
            return canAddAlias.FirstError;
        }

        var addPlayerAlias = _aliasStorageService.AddPlayerAlias(player, alias, command);
        if (addPlayerAlias.IsError)
        {
            return addPlayerAlias.FirstError;
        }

        return Result.Success;
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

        // If any arg option, combine all args to one
        if (registeredCommand.ArgOptions
                .Any(o => o.Args.Length == 1 && o.Args.First().Type == ArgType.Any) &&
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
                {
                    _messagingService.MsgToPlayerChat(player, helpText);
                }

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

        if (AdminManager.PlayerHasPermissions(player, Permissions.Flags.Root) ||
            AdminManager.PlayerHasPermissions(player, Permissions.Flags.Rcon))
        {
            return true;
        }

        var requiredGroups = registeredCommand.RequiredFlags.Where(f => f.StartsWith("#")).ToList();
        if (requiredGroups.Count > 0)
        {
            var playerGotRequiredGroups = requiredGroups.Count(g => AdminManager.PlayerInGroup(player, g)) ==
                                          requiredGroups.Count;
            if (playerGotRequiredGroups)
            {
                return true;
            }
        }

        var requiredFlags = registeredCommand.RequiredFlags.Where(f => f.StartsWith("@")).ToList();
        if (requiredFlags.Count > 0)
        {
            var playerGotRequiredGroups = requiredFlags.Count(f => AdminManager.PlayerHasPermissions(player, f)) ==
                                          requiredFlags.Count;
            if (playerGotRequiredGroups)
            {
                return true;
            }
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
        {
            if (player.UserId == userId)
            {
                return player;
            }
        }

        return Errors.PlayerNullOrNotValid();
    }

    #endregion

    #region GetRegisteredCommand

    private ErrorOr<RegisteredCommand> GetRegisteredCommand(CCSPlayerController player, string command)
    {
        if (_registeredCommands.TryGetValue(command, out var registeredCommand))
        {
            return registeredCommand;
        }

        var commandForGlobalAlias = _aliasStorageService.GetCommandForGlobalAlias(command);
        if (commandForGlobalAlias.IsError == false)
        {
            if (_registeredCommands.TryGetValue(commandForGlobalAlias.Value, out registeredCommand))
            {
                return registeredCommand;
            }
        }

        var commandForPlayerAlias = _aliasStorageService.GetCommandForPlayerAlias(player, command);
        if (commandForPlayerAlias.IsError == false)
        {
            if (_registeredCommands.TryGetValue(commandForPlayerAlias.Value, out registeredCommand))
            {
                return registeredCommand;
            }
        }

        return Error.NotFound();
    }

    #endregion
}