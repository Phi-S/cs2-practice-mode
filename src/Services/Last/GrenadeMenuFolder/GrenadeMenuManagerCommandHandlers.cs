using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.CommandFolder;
using Cs2PracticeMode.Services.Second.GrenadeStorageFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services.Last.GrenadeMenuFolder;

public partial class GrenadeMenuService
{
    private ErrorOr<Success> CommandActionOpenNadeMenu(CCSPlayerController player, CommandInfo commandInfo)
    {
        var currentMap = Server.MapName.ToLower().Trim();
        var grenades = _grenadeStorageService
            .GetWhere(g => g.Map.ToLower().Trim().Equals(currentMap));

        Console.WriteLine($"map name: {currentMap} | grenades count: {grenades.Count}");
        var grenadeMenu = GenerateGrenadeMenu("Grenade menu", grenades);
        var openHtmlMenu = _messagingService.OpenHtmlMenu(player, grenadeMenu);
        if (openHtmlMenu.IsError)
        {
            return openHtmlMenu.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandActionSave(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var grenadeName = arg.Value;
        var lastThrownGrenade = _lastThrownGrenadeService.GetLastThrownGrenade(player);
        if (lastThrownGrenade.IsError)
        {
            return Errors.Fail(lastThrownGrenade.ErrorMessage());
        }

        var grenade = lastThrownGrenade.Value;
        grenade.Name = grenadeName;
        grenade.PlayerSteamId = player.SteamID;

        var jsonGrenade = GrenadeJsonModel.FromGrenade(grenade);
        if (jsonGrenade.IsError)
        {
            return jsonGrenade.FirstError;
        }

        var addResult = _grenadeStorageService.Add(jsonGrenade.Value);
        if (addResult.IsError)
        {
            return addResult.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Successfully saved grenade \"{grenadeName}\"");
        return Result.Success;
    }


    private ErrorOr<Success> CommandActionDelete(CCSPlayerController player, CommandInfo commandInfo)
    {
        ErrorOr<Deleted> deleteResult;
        if (commandInfo.GotArgsCount(0))
        {
            var grenade = GetSelectedNade(player);
            if (grenade.IsError)
            {
                return grenade.FirstError;
            }

            deleteResult = _grenadeStorageService.Delete(grenade.Value.Id);
        }
        else
        {
            var arg = commandInfo.GetArgString();
            if (arg.IsError)
            {
                return arg.FirstError;
            }

            var name = arg.Value.ToLower().Trim();
            var map = Server.MapName.ToLower().Trim();
            var grenadeForName = _grenadeStorageService.Get(name, map);
            if (grenadeForName.IsError)
            {
                return grenadeForName.FirstError;
            }

            deleteResult = _grenadeStorageService.Delete(grenadeForName.Value.Id);
        }

        if (deleteResult.IsError)
        {
            return deleteResult.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, "Grenade deleted");
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionFind(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var nameToFind = arg.Value.ToLower().Trim();
        var currentMap = Server.MapName;
        var grenades = _grenadeStorageService
            .GetWhere(g => g.Map.Equals(currentMap) && g.Map.Contains(nameToFind))
            .ToList();

        var grenadeMenu = GenerateGrenadeMenu("Found grenades", grenades);
        var openHtmlMenu = _messagingService.OpenHtmlMenu(player, grenadeMenu);
        if (openHtmlMenu.IsError)
        {
            return openHtmlMenu.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandActionEditNade(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var grenadeName = arg.Value;
        var getResult = _grenadeStorageService.Get(grenadeName, Server.MapName);
        if (getResult.IsError)
        {
            return getResult.FirstError;
        }

        var selectNade = SelectNade(player, getResult.Value);
        if (selectNade.IsError)
        {
            return selectNade.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Grenade \"{grenadeName}\" selected");
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerRename(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var newName = arg.Value;

        var selectedNadeResult = GetSelectedNade(player);
        if (selectedNadeResult.IsError)
        {
            return selectedNadeResult.FirstError;
        }

        var selectedNade = selectedNadeResult.Value;
        var nameToReplace = selectedNade.Name;
        selectedNade.Name = newName;

        var replace = _grenadeStorageService.Update(selectedNade);
        if (replace.IsError)
        {
            return replace.FirstError;
        }

        var selectNade = SelectNade(player, selectedNade);
        if (selectNade.IsError)
        {
            return selectNade.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Grenade name \"{nameToReplace}\" updated to \"{newName}\"");
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerDescription(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var newDescription = arg.Value;

        var selectedNadeResult = GetSelectedNade(player);
        if (selectedNadeResult.IsError)
        {
            return selectedNadeResult.FirstError;
        }

        var selectedNade = selectedNadeResult.Value;
        var oldDescription = selectedNade.Description;
        selectedNade.Description = newDescription;

        var replace = _grenadeStorageService.Update(selectedNade);
        if (replace.IsError)
        {
            return replace.FirstError;
        }

        _messagingService.MsgToPlayerChat(player,
            $"Grenade description of \"{selectedNade.Name}\" updated from {oldDescription} to \"{selectedNade.Description}\"");
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerShowGlobalGrenadeTags(CCSPlayerController player, CommandInfo commandInfo)
    {
        var currentMap = Server.MapName;
        var allGrenades = _grenadeStorageService.GetWhere(g => g.Map.Equals(currentMap));
        var tags = new List<string>();
        foreach (var grenade in allGrenades) tags.AddRange(grenade.Tags.Select(t => t.ToLower().Trim()));

        tags = tags.Distinct().ToList();
        _messagingService.MsgToPlayerChat(player,
            $"Currently available tags: {ChatColors.Green}{string.Join(", ", tags)}");
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerAddTag(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var tag = arg.Value;
        var selectedNadeResult = GetSelectedNade(player);
        if (selectedNadeResult.IsError)
        {
            return selectedNadeResult.FirstError;
        }

        var selectedNade = selectedNadeResult.Value;


        if (selectedNade.Tags.Contains(tag))
        {
            return Errors.Fail("Tag already added");
        }

        selectedNade.Tags.Add(tag);

        var replace = _grenadeStorageService.Update(selectedNade);
        if (replace.IsError)
        {
            return replace.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Added tag \"{tag}\" to grenade \"{selectedNade.Name}\"");
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerRemoveTag(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var tagToRemove = arg.Value;
        var selectedNadeResult = GetSelectedNade(player);
        if (selectedNadeResult.IsError)
        {
            return selectedNadeResult.FirstError;
        }

        var selectedNade = selectedNadeResult.Value;
        var removeTag = selectedNade.RemoveTag(tagToRemove);
        if (removeTag.IsError)
        {
            return removeTag.FirstError;
        }

        tagToRemove = tagToRemove.ToLower().Trim();
        for (var i = selectedNade.Tags.Count - 1; i >= 0; i--)
        {
            var tag = selectedNade.Tags[i].ToLower().Trim();
            if (tag.Equals(tagToRemove))
            {
                selectedNade.Tags.RemoveAt(i);

                var replace = _grenadeStorageService.Update(selectedNade);
                if (replace.IsError)
                {
                    return replace.FirstError;
                }

                _messagingService.MsgToPlayerChat(player,
                    $"Removed tag \"{tagToRemove}\" from grenade \"{selectedNade.Name}\"");
                return Result.Success;
            }
        }

        return Errors.Fail($"Grenade is not tagged with \"{tagToRemove}\"");
    }

    private ErrorOr<Success> CommandHandlerClearTags(CCSPlayerController player, CommandInfo commandInfo)
    {
        var selectedNadeResult = GetSelectedNade(player);
        if (selectedNadeResult.IsError)
        {
            return selectedNadeResult.FirstError;
        }

        var selectedNade = selectedNadeResult.Value;
        selectedNade.Tags.Clear();

        var replace = _grenadeStorageService.Update(selectedNade);
        if (replace.IsError)
        {
            return replace.FirstError;
        }

        _messagingService.MsgToPlayerChat(player, $"Removed all tags from grenade \"{selectedNade.Name}\"");
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerDeleteTag(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var tagToDelete = arg.Value.ToLower().Trim();
        var currentMap = Server.MapName;
        var allGrenadesWithTag = _grenadeStorageService.GetWhere(g =>
            g.Map.Equals(currentMap) && g.Tags.Any(t => t.ToLower().Trim().Equals(tagToDelete)));
        foreach (var grenade in allGrenadesWithTag)
            if (grenade.RemoveTag(tagToDelete).IsError == false)
            {
                var update = _grenadeStorageService.Update(grenade);
                if (update.IsError)
                {
                    return update.FirstError;
                }
            }

        _messagingService.MsgToPlayerChat(player, $"Removed tag {tagToDelete} from all grenades");
        return Result.Success;
    }
}