# cs2-practice-mode

---

## Installation

### Download and install [Metamod](https://www.sourcemm.net/downloads.php/?branch=master)

- [How to install Metamod](https://wiki.alliedmods.net/Installing_Metamod:Source)
- <em>Tested with version 2.0.0-git1290</em>

### Download and install [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)

- [How to install CounterStrikeSharp](https://docs.cssharp.dev/docs/guides/getting-started.html)
- <em>Tested with the version v228</em>

### Download and install [cs2-practice-mode](https://github.com/Phi-S/cs2-practice-mode)

- Download the latest version of [here](https://github.com/Phi-S/cs2-practice-mode/releases)
- Extract folder
- Move the extracted folder in `/game/csgo/addons/counterstrikesharp/plugins`.

---

## Storage

The storage destination of saved grenades/settings etc. can be set to either local storage or postgres.

If nothing is specified, the plugin is using local storage.
All stored jsons will be put in the plugin default storage
folder `/game/csgo/addons/counterstrikesharp/plugins/Cs2PracticeMode/storage`.

You can use the `DataLocation` setting in
the `Cs2PracticeMode.json`(`/game/csgo/addons/counterstrikesharp/configs/plugins/Cs2PracticeMode`) to change the local
storage folder or switch to postgres by providing a connection string.

### Example `DataLocation` settings:

- `local#/home/storage`
- `postgres#Host=127.0.0.1:32768;Database=cs2-practice-mode;Username=postgres;Password=123`

---

## Commands

| Server                                 |                                           |
|----------------------------------------|-------------------------------------------|
| help                                   | Print all available commands              |
| help [command(String)]                 | Print help for command                    |
| map [map(String)]                      | Change map                                |
| settings                               | Opens the settings menu                   |
| rconlogin [fake rcon password(String)] | Login to get temporally admin permissions |
| rcon [command(Any)]                    | Executes a rcon command                   |

| Grenades                       |                                                                                                           |
|--------------------------------|-----------------------------------------------------------------------------------------------------------|
| rethrow                        | Rethrows the last thrown grenade                                                                          |
| last                           | Teleports to the last thrown grenade position                                                             |
| back                           | Teleports to the previous grenade in the list of thrown grenade                                           |
| back [amount(UInt)]            | Moves [amount] position backward in the list of thrown grenades and teleports to the grenade at that spot |
| forward                        | Teleports to next grenade in the list of thrown grenade                                                   |
| forward [amount(UInt)]         | Moves [amount] position forward in the list of thrown grenades and teleports to the grenade at that spot  |
| nades                          | Open global grenade menu                                                                                  |
| save [name(String)]            | Saves the last thrown grenade                                                                             |
| delete [name(String)]          | Delete saved grenade                                                                                      |
| throw                          | Throw the selected grenade                                                                                |
| throw [grenade name(String)]   | Throw and select grenade by name                                                                          |
| throw [grenade id(UInt)]       | Throw and select grenade by id                                                                            |
| select [grenade name(String)]  | Select grenade to edit/throw by name                                                                      |
| select [grenade id(UInt)]      | Select grenade to edit/throw by id                                                                        |
| find [name(String)]            | Find grenade with specific name                                                                           |
| rename [new name(String)]      | Rename selected grenade                                                                                   |
| desc [new description(String)] | Change the description of the selected grenade                                                            |
| tags                           | Show all global grenade tags                                                                              |
| addtag [tag(String)]           | Add tag to selected grenade                                                                               |
| removetag [tag(String)]        | Remove tag from selected grenade                                                                          |
| cleartags                      | Remove all tags from selected grenade                                                                     |
| deletetag [tag(String)]        | Remove tag from every grenade                                                                             |
| clear                          | Clear all grenades thrown by player                                                                       |
| clearall                       | Clear all grenades on the map                                                                             |

| Bots      |                                                 |
|-----------|-------------------------------------------------|
| bot       | Place bot on current position                   |
| tbot      | Place terrorist bot on current position         |
| ctbot     | Place counter-terrorist bot on current position |
| cbot      | Place crouching bot on current position         |
| boost     | Boost on bot                                    |
| cboost    | Boost on crouching bot                          |
| nobot     | Remove closest bot                              |
| nobots    | Remove all bots placed by player                |
| clearbots | Remove all bots                                 |
| swapbot   | Swap position with closest bot                  |
| movebot   | Move closest bot to current position            |

| Spawns                         |                                                            |
|--------------------------------|------------------------------------------------------------|
| spawn [spawn number(UInt)]     | Teleport to spawn                                          |
| tspawn [t spawn number(UInt)]  | Teleport to t spawn                                        |
| ctspawn [t spawn number(UInt)] | Teleport to ct spawn                                       |
| bestspawn                      | Teleport to the best spawn based on your current position  |
| worstspawn                     | Teleport to the worst spawn based on your current position |

| Alias                                         |                                              |
|-----------------------------------------------|----------------------------------------------|
| alias [alias(String)]                         | Displays the command behind the alias        |
| alias [alias(String)] [command(String)]       | Creates new player alias                     |
| removealias [alias(String)]                   | Removes player alias                         |
| globalalias [alias(String)]                   | Displays the command behind the global alias |
| globalalias [alias(String)] [command(String)] | Creates new global alias                     |
| removeglobalalias [alias(String)]             | Removes global alias                         |

| Misc                         |                                                                                                 |
|------------------------------|-------------------------------------------------------------------------------------------------|
| break                        | Break all breakable props on the map                                                            |
| noflash                      | Turns noflash on or of                                                                          |
| timer                        | Start timer. If you start moving, the timer will start. If you stop moving, the timer will stop |
| timer2                       | Start or stop timer                                                                             |
| countdown [duration(Double)] | Starts a countdown                                                                              |
| flash                        | Starts the flashing mode                                                                        |
| stop                         | Stops the flashing mode                                                                         |

---

## Permissions

- The usage of the commands above can be limited with permission flags using
  the [CounterStrikeSharp permission system](https://docs.cssharp.dev/docs/admin-framework/defining-admin-groups.html).

| Flag                               | |
  |------------------------------------|-|
| @Cs2PracticeMode/root              | |
| @Cs2PracticeMode/rcon              | |
| @Cs2PracticeMode/map               | |
| @Cs2PracticeMode/settings          | |
| @Cs2PracticeMode/last              | |
| @Cs2PracticeMode/rethrow           | |
| @Cs2PracticeMode/forward           | |
| @Cs2PracticeMode/back              | |
| @Cs2PracticeMode/nades             | |
| @Cs2PracticeMode/writenades        | |
| @Cs2PracticeMode/clear             | |
| @Cs2PracticeMode/clearall          | |
| @Cs2PracticeMode/alias             | |
| @Cs2PracticeMode/removealias       | |
| @Cs2PracticeMode/globalalias       | |
| @Cs2PracticeMode/removeglobalalias | |
| @Cs2PracticeMode/bot               | |
| @Cs2PracticeMode/clearbots         | |
| @Cs2PracticeMode/spawn             | |
| @Cs2PracticeMode/countdown         | |
| @Cs2PracticeMode/flash             | |
| @Cs2PracticeMode/noflash           | |
| @Cs2PracticeMode/timer             | |
