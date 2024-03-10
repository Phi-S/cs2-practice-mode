﻿using Cs2PracticeMode.Storage.Single;

namespace Cs2PracticeMode.Services.Last.SettingsFolder;

public class Settings : ICloneable, IData
{
    public bool DisableSmokeColors { get; set; }
    public bool DisableBlindTimePrint { get; set; }
    public bool DisableDamagePrint { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }

    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}