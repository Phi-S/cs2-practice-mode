using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Helpers.UnitTestFolderFolder;

public static class UnitTestFolderHelper
{
    public static string GetNewUnitTestFolder(ITestOutputHelper outputHelper)
    {
        var guid = Guid.NewGuid();
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "cs2-practice-mode", guid.ToString());
        Directory.CreateDirectory(folder);
        outputHelper.WriteLine($"Test folder: {folder}");
        return folder;
    }
}