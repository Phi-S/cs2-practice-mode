using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.CommandFolder;
using Cs2PracticeMode.Services.Second.MessagingFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services.Last.AliasFolder;

public class AliasService : Base
{
    private readonly CommandService _commandService;
    private readonly MessagingService _messagingService;

    public AliasService(CommandService commandService,
        MessagingService messagingService)
    {
        _commandService = commandService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        _commandService.RegisterCommand(ChatCommands.Alias,
            CommandActionRegisterPlayerAlias,
            ArgOption.OneString("Displays the command behind the alias", "alias"),
            ArgOption.TwoStrings("Creates new player alias", "alias", "command"),
            Permissions.Flags.Alias);

        _commandService.RegisterCommand(ChatCommands.RemoveAlias,
            CommandActionDeregisterPlayerAlias,
            ArgOption.OneString("Removes player alias", "alias"),
            Permissions.Flags.RemoveAlias);

        _commandService.RegisterCommand(ChatCommands.GlobalAlias,
            CommandActionRegisterGlobalAlias,
            ArgOption.OneString("Displays the command behind the global alias", "alias"),
            ArgOption.TwoStrings("Creates new global alias", "alias", "command"),
            Permissions.Flags.GlobalAlias);

        _commandService.RegisterCommand(ChatCommands.RemoveGlobalAlias,
            CommandActionDeregisterGlobalAlias,
            ArgOption.OneString("Removes global alias", "alias"),
            Permissions.Flags.RemoveGlobalAlias);

        base.Load(plugin);
    }

    #region CommandActions

    private ErrorOr<Success> CommandActionRegisterPlayerAlias(CCSPlayerController player, CommandInfo commandInfo)
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
            var aliasCommand = _commandService.GetAliasCommand(alias, player);
            if (aliasCommand.IsError)
            {
                return aliasCommand.FirstError;
            }

            _messagingService.MsgToPlayerChat(player,
                $"The alias \"{alias}\" is pointing to the command \"{aliasCommand.Value}\"");
            return Result.Success;
        }


        var stringStringArg = commandInfo.GetArgStringString();
        if (stringStringArg.IsError)
        {
            return stringStringArg.FirstError;
        }

        alias = stringStringArg.Value.Item1;
        var command = stringStringArg.Value.Item2;
        var registerAliasResult = _commandService.RegisterAlias(alias, command, player);
        if (registerAliasResult.IsError)
        {
            return registerAliasResult.FirstError;
        }

        _messagingService.MsgToPlayerChat(player,
            $"Player alias \"{alias}\" with the command \"{command}\" registered");
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
        var deregisterAlias = _commandService.DeregisterAlias(alias, player);
        if (deregisterAlias.IsError)
        {
            return deregisterAlias.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Personal alias \"{alias}\" removed");
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionRegisterGlobalAlias(CCSPlayerController player, CommandInfo commandInfo)
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
            var aliasCommand = _commandService.GetAliasCommand(alias);
            if (aliasCommand.IsError)
            {
                return aliasCommand.FirstError;
            }

            _messagingService.MsgToPlayerChat(player,
                $"The alias \"{alias}\" is pointing to the command \"{aliasCommand.Value}\"");
            return Result.Success;
        }


        var stringStringArg = commandInfo.GetArgStringString();
        if (stringStringArg.IsError)
        {
            return stringStringArg.FirstError;
        }

        alias = stringStringArg.Value.Item1;
        var command = stringStringArg.Value.Item2;
        var registerAliasResult = _commandService.RegisterAlias(alias, command);
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
        var deregisterAlias = _commandService.DeregisterAlias(alias);
        if (deregisterAlias.IsError)
        {
            return deregisterAlias.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Global alias \"{alias}\" removed");
        return Result.Success;
    }

    #endregion
}