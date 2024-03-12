using Cs2PracticeMode.Constants;
using ErrorOr;

namespace Cs2PracticeMode.Services._3.LastThrownGrenadeFolder;

public class GrenadeHistory
{
    private readonly List<Grenade> _history = new();
    private readonly object _historyLock = new();

    // The current index used for .back .forward
    private uint _currentIndex;

    /// <summary>
    ///     Creates new GrenadeHistory for player
    /// </summary>
    /// <param name="snapshot">0 entry of history</param>
    public GrenadeHistory(Grenade snapshot)
    {
        lock (_historyLock)
        {
            _history.Add(snapshot);
            _currentIndex = 0;
        }
    }

    public Grenade GetLatestSnapshotInHistory()
    {
        lock (_historyLock)
        {
            return _history.Last();
        }
    }

    public Grenade GetFirstSnapshotInHistory()
    {
        lock (_historyLock)
        {
            return _history.First();
        }
    }

    public void AddNewEntry(Grenade snapshot)
    {
        lock (_historyLock)
        {
            _history.Add(snapshot);
        }
    }

    /// <summary>
    ///     Go forward in history
    /// </summary>
    /// <param name="count">the amount of grenades to go forward in history</param>
    /// <returns>The new position in history</returns>
    public ErrorOr<Grenade> Forward(uint count = 1)
    {
        lock (_historyLock)
        {
            if (_currentIndex == _history.Count - 1)
            {
                return Errors.Fail("Reached the latest entry in your grenade history");
            }

            _currentIndex += count;
            return _history[(int)_currentIndex];
        }
    }

    /// <summary>
    ///     Go back in history
    /// </summary>
    /// <param name="count">the amount of grenades to go back in history</param>
    /// <returns>The new position in history</returns>
    public ErrorOr<Grenade> Back(uint count = 1)
    {
        lock (_historyLock)
        {
            if (_currentIndex == 0)
            {
                return Errors.Fail("Reached the first entry in your grenade history");
            }

            _currentIndex -= count;
            return _history[(int)_currentIndex];
        }
    }
}