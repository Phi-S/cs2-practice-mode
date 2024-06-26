﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._0.PluginConfigFolder;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services._3.FakeRconFolder;

public class FakeRconService : Base
{
    private readonly CommandService _commandService;
    private readonly MessagingService _messagingService;
    private readonly PluginConfigService _pluginConfigService;

    public FakeRconService(CommandService commandService,
        PluginConfigService pluginConfigService,
        MessagingService messagingService)
    {
        _commandService = commandService;
        _pluginConfigService = pluginConfigService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        _commandService.RegisterCommand(ChatCommands.RconLogin,
            CommandActionRconLogin,
            [ArgOption.String("Login to get temporally admin permissions", "fake rcon password")],
            Array.Empty<string>());

        _commandService.RegisterCommand(ChatCommands.Rcon,
            CommandActionFakeRcon,
            [ArgOption.Any("Executes a rcon command", "command")],
            [Permissions.Flags.Rcon]);

        base.Load(plugin);
    }

    private ErrorOr<Success> CommandActionRconLogin(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (_pluginConfigService.Config.EnableFakeRcon == false)
        {
            return Errors.Fail("Fake rcon is disabled");
        }

        var argString = commandInfo.GetArgString();
        if (argString.IsError)
        {
            return argString.FirstError;
        }

        if (argString.Equals(_pluginConfigService.Config.FakeRconPassword) == false)
        {
            return Errors.Fail("Wrong fake rcon password");
        }

        AdminManager.AddPlayerPermissions(player, Permissions.Flags.Rcon);
        _messagingService.MsgToPlayerChat(player, "You are not admin");
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionFakeRcon(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (_pluginConfigService.Config.EnableFakeRcon == false)
        {
            return Errors.Fail("Fake rcon is disabled");
        }

        var command = commandInfo.GetArgString();
        if (command.IsError)
        {
            return command.FirstError;
        }

        Server.ExecuteCommand(command.Value);
        _messagingService.MsgToPlayerChat(player, $"Command \"{command.Value}\" executed");
        return Result.Success;
    }
}