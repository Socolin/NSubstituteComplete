using JetBrains.Annotations;
using JetBrains.Application.UI.Options;
using JetBrains.IDE.UI.Extensions;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.UnitTestFramework.UI.Options.ViewModel;
using JetBrains.Threading;

namespace ReSharperPlugin.NSubstituteComplete.Options;

public class MockAliasesModel(
    Lifetime lifetime,
    [NotNull] GroupingEventHosts hosts,
    [NotNull] OptionsSettingsSmartContext smartContext
) : BeTreeGridExtensions.DictionaryModel<string, string>.FromScalar<string>(
    lifetime,
    hosts,
    smartContext,
    smartContext.Schema.GetScalarEntry<NSubstituteCompleteSettings, string>(s => s.MockAliases),
    SemicolonSeparatedPairSerializer.Deserialize,
    SemicolonSeparatedPairSerializer.Serialize
)
{
    public override Entry GetNewEntry() => new(myLifetime, mySaveRequested.Incoming, string.Empty, string.Empty);
}
