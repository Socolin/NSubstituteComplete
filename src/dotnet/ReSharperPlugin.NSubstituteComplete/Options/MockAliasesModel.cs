using JetBrains.Annotations;
using JetBrains.Application.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.UnitTestFramework.UI.Options.ViewModel;

namespace ReSharperPlugin.NSubstituteComplete.Options
{
    public class MockAliasesModel : DictionaryModel<string, string>.Scalar
    {
        public MockAliasesModel(Lifetime lifetime, [NotNull] OptionsSettingsSmartContext smartContext)
            : base(lifetime, smartContext, smartContext.Schema.GetScalarEntry<NSubstituteCompleteSettings, string>(s => s.MockAliases))
        {
        }

        public override Entry GetNewEntry() => new Entry(myLifetime, mySaveRequested.Incoming, string.Empty, string.Empty);
    }
}
