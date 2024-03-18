using System.Text;

namespace Cs2PracticeModeTests.Helpers.RandomHelperFolder;

public static class RandomHelper
{
    public static uint RandomUInt()
    {
        var random = new Random();
        return (uint) random.Next();
    }

    public static long RandomLong()
    {
        var random = new Random();
        return random.NextInt64();
    }

    public static string RandomString(int length = 20)
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var sb = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            sb.Append(chars[random.Next(chars.Length)]);
        }

        return sb.ToString().ToLower();
    }
}