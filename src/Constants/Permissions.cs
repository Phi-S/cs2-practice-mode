namespace Cs2PracticeMode.Constants;

public static class Permissions
{
    private const string Domain = "cs2pracmode";

    private static string GetFlag(string flag)
    {
        return $"@{Domain}/{flag}";
    }

    public static class Groups
    {
        public static readonly string Root = GetGroup("root");

        private static string GetGroup(string group)
        {
            return $"#{Domain}/{group}";
        }
    }

    public static class Flags
    {
        public static readonly string Alias = GetFlag("alias");
        public static readonly string RemoveAlias = GetFlag("ralias");
        public static readonly string GlobalAlias = GetFlag("galias");
        public static readonly string RemoveGlobalAlias = GetFlag("ralias");

        public static readonly string Bot = GetFlag("bot");
        public static readonly string ClearBots = GetFlag("ClearBots");

        public static readonly string Break = GetFlag("break");

        public static readonly string ChangeMap = GetFlag("map");

        public static readonly string ClearPlayerGrenades = GetFlag("clear");
        public static readonly string ClearAllGrenades = GetFlag("clearall");

        public static readonly string Countdown = GetFlag("countdown");

        public static readonly string Rcon = GetFlag("rcon");

        public static readonly string FlashMode = GetFlag("flash");

        public static readonly string ReadGrenades = GetFlag("nades");
        public static readonly string WriteGrenades = GetFlag("writenades");

        public static readonly string Rethrow = GetFlag("rethrow");
        public static readonly string Last = GetFlag("last");
        public static readonly string Forward = GetFlag("forward");
        public static readonly string Back = GetFlag("back");

        public static readonly string NoFlash = GetFlag("noflash");

        public static readonly string Settings = GetFlag("settings");

        public static readonly string Spawn = GetFlag("spawn");

        public static readonly string SwapTeam = GetFlag("swap");

        public static readonly string Timer = GetFlag("timer");
    }
}