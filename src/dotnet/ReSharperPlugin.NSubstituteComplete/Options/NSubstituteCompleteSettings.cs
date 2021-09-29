using JetBrains.Application.Settings;
using JetBrains.ReSharper.UnitTestFramework.Settings;

namespace ReSharperPlugin.NSubstituteComplete.Options
{
    [SettingsKey(typeof(UnitTestingSettings), "Settings for NSubstituteComplete")]
    public class NSubstituteCompleteSettings
    {
        [SettingsEntry("", "Mock aliases")]
        public string MockAliases { get; set; }
    }
}
