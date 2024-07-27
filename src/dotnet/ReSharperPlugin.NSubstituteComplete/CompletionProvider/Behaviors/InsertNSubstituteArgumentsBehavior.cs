using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors;

public class InsertNSubstituteArgumentsBehavior(NSubstituteArgumentsInformation info)
    : TextualBehavior<NSubstituteArgumentsInformation>(info)
{
    public override void Accept(
        ITextControl textControl,
        DocumentRange nameRange,
        LookupItemInsertType insertType,
        Suffix suffix,
        ISolution solution,
        bool keepCaretStill
    )
    {
        var psiServices = solution.GetPsiServices();
        var invocationExpression = TextControlToPsi.GetElement<IInvocationExpression>(solution, textControl).NotNull();
        using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "Insert arguments"))
        {
            var factory = CSharpElementFactory.GetInstance(invocationExpression);
            var newInvocation = (IInvocationExpression)factory.CreateExpression("a(" + string.Join(", ", Info.Types.Select((_, i) => $"Arg.{Info.ArgSuffix}<${i}>()")) + ")", Info.Types.OfType<object>().ToArray());
            invocationExpression.SetArgumentList(newInvocation.ArgumentList);
        }
    }
}
