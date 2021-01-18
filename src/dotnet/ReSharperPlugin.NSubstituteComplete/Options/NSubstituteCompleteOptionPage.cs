using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.IDE.UI;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Extensions.Properties;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.UI.Automation;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.UI.Options.ViewModel;
using JetBrains.Rider.Model.UIAutomation;

namespace ReSharperPlugin.NSubstituteComplete.Options
{
    [OptionsPage(Id,
        PageTitle,
        null,
        ParentId = UnitTestingPages.General,
        NestingType = OptionPageNestingType.Inline,
        IsAlignedWithParent = true,
        Sequence = 0.1d)]
    public class NSubstituteCompleteOptionPage : CustomSimpleOptionsPage
    {
        private new const string Id = nameof(NSubstituteCompleteOptionPage);
        private const string PageTitle = "NSubstiteComplete";

        public NSubstituteCompleteOptionPage(
            Lifetime lifetime,
            [NotNull] OptionsSettingsSmartContext smartContext,
            [NotNull] OptionsPageContext pageContext,
            [NotNull] IconHostBase iconHost,
            [NotNull] IShellLocks locks,
            [CanBeNull] ISolution solution,
            bool wrapInScrollablePanel = false
        )
            : base(lifetime, smartContext)
        {
            AddHeader("NSubstituteComplete");
            AddCustomOption(MockAliases(lifetime, smartContext, pageContext, iconHost, locks, solution));
        }

        private BeControl MockAliases(
            Lifetime lifetime,
            OptionsSettingsSmartContext smartContext,
            OptionsPageContext pageContext,
            IconHostBase iconHost,
            IShellLocks locks,
            [CanBeNull] ISolution solution
        )
        {
            var model = new MockAliasesModel(lifetime, smartContext);
            var beToolbar = model.SelectedEntry.GetBeSingleSelectionListWithToolbar(model.Entries,
                    lifetime,
                    (entryLt, line, properties) => new List<BeControl>
                    {
                        solution == null ? line.Name.GetBeTextBox(entryLt) : line.Name.GetBeTextBox(entryLt).WithTypeCompletion(solution, lifetime, CSharpLanguage.Instance),
                        solution == null ? line.Value.GetBeTextBox(entryLt) : line.Value.GetBeTextBox(entryLt).WithTypeCompletion(solution, lifetime, CSharpLanguage.Instance)
                    },
                    iconHost,
                    new[] {"Type (interface),*", "Alias,*"},
                    dock: BeDock.RIGHT)
                .AddButtonWithListAction(BeListAddAction.ADD, i => model.GetNewEntry(), customTooltip: "Add")
                .AddButtonWithListAction<StringDictionaryEntry>(BeListAction.REMOVE, i => model.RemoveSelectedEntry(), customTooltip: "Remove");

            if (!pageContext.IsRider)
                beToolbar.BindToLocalProtocol(lifetime, locks);

            return beToolbar.WithMinSize(BeControlSizes.GetSize(height: BeControlSizeType.SMALL), lifetime);
        }
    }
}
