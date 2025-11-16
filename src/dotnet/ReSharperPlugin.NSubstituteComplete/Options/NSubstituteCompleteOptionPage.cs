using System.Runtime.InteropServices;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.IDE.UI;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Extensions.Properties;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.RdVerification;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.UI.Automation;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.UnitTestFramework.UI.Options;
using JetBrains.Rider.Model.UIAutomation;

namespace ReSharperPlugin.NSubstituteComplete.Options;

[OptionsPage(Id,
    PageTitle,
    null,
    ParentId = UnitTestingPages.General,
    NestingType = OptionPageNestingType.Inline,
    Sequence = 0.1d
)]
public class NSubstituteCompleteOptionPage : BeSimpleOptionsPage
{
    private new const string Id = nameof(NSubstituteCompleteOptionPage);
    private const string PageTitle = "NSubstiteComplete";

    public NSubstituteCompleteOptionPage(
        Lifetime lifetime,
        [NotNull] IThreading threading,
        [NotNull] OptionsPageContext optionsPageContext,
        [NotNull] OptionsSettingsSmartContext optionsSettingsSmartContext,
        [NotNull] OptionsSettingsSmartContext smartContext,
        [NotNull] OptionsPageContext pageContext,
        [NotNull] IconHostBase iconHost,
        [NotNull] IShellLocks locks,
        [Optional] ISolution solution,
        bool wrapInScrollablePanel = false
    )
        : base(lifetime, optionsPageContext, optionsSettingsSmartContext, wrapInScrollablePanel)
    {
        AddHeader("NSubstituteComplete");
        AddControl(MockAliases(lifetime, threading, smartContext, pageContext, iconHost, locks, solution));
    }

    private static BeControl MockAliases(
        Lifetime lifetime,
        IThreading threading,
        OptionsSettingsSmartContext smartContext,
        OptionsPageContext pageContext,
        IconHostBase iconHost,
        IShellLocks locks,
        [CanBeNull] ISolution solution
    )
    {
        var model = new MockAliasesModel(lifetime, threading.GroupingEvents, smartContext);
        var beToolbar = model.SelectedEntry.GetBeSingleSelectionListWithToolbar(model.Entries,
                lifetime,
                (lt, line, _) =>
                [
                    solution == null ? line.Name.GetBeTextBox(lt) : line.Name.GetBeTextBox(lt).WithTypeCompletion(solution, lt, CSharpLanguage.Instance),
                    solution == null ? line.Value.GetBeTextBox(lt) : line.Value.GetBeTextBox(lt).WithTypeCompletion(solution, lt, CSharpLanguage.Instance),
                ],
                iconHost,
                ["Type (interface),*", "Alias,*"],
                dock: BeDock.RIGHT
            )
            .AddButtonWithListAction(BeListAddAction.ADD, _ => model.GetNewEntry(), customTooltip: "Add")
            .AddButtonWithListAction<BeTreeGridExtensions.DictionaryModel<string, string>.Entry>(BeListAction.REMOVE, _ => model.RemoveSelectedEntry(), customTooltip: "Remove");
        if (!pageContext.IsRider)
            beToolbar.BindToLocalProtocol(lifetime, locks);

        return beToolbar.WithMinSize(BeControlSizes.GetSize(height: BeControlSizeType.SMALL), lifetime);
    }
}
