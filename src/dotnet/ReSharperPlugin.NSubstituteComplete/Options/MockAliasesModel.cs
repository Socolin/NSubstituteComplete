using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Options;
using JetBrains.IDE.UI.Extensions;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.UnitTestFramework.Settings;
using JetBrains.Threading;

namespace ReSharperPlugin.NSubstituteComplete.Options
{
    public class MockAliasesModel : BeTreeGridExtensions.DictionaryModel<string, string>.FromIndexed
    {
        public MockAliasesModel(
            Lifetime lifetime,
            [NotNull] GroupingEventHosts hosts,
            [NotNull] OptionsSettingsSmartContext smartContext
        )
            : base(lifetime, hosts, smartContext, smartContext.Schema.GetIndexedEntry<UnitTestRunnerSettings, IIndexedEntry<string, string>>(s => s.EnvironmentVariablesIndexed))
        {
        }

        public override Entry GetNewEntry() => new(this.myLifetime, this.mySaveRequested.Incoming, string.Empty, string.Empty);
    }
}
