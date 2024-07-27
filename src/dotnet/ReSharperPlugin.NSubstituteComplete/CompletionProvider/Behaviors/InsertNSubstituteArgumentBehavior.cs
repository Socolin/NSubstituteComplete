using System;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors;

public class InsertNSubstituteArgumentBehavior(NSubstituteArgumentInformation info, Func<NSubstituteArgumentInformation, CSharpElementFactory, ICSharpExpression> createExpressionFn)
    : TextualBehavior<NSubstituteArgumentInformation>(info)
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
        var element = GetTargetElementToReplace(solution, textControl);

        if (element is ICSharpArgument argumentExpression)
        {
            ICSharpArgument addedArgument;
            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "Insert arguments"))
            {
                var factory = CSharpElementFactory.GetInstance(argumentExpression);
                var expression = createExpressionFn(Info, factory);
                addedArgument = argumentExpression.ReplaceBy(factory.CreateArgument(ParameterKind.VALUE, expression));
            }

            textControl.Caret.MoveTo(addedArgument.GetDocumentEndOffset().Shift(Info.InsertCaretOffset), CaretVisualPlacement.Generic);
        }
        else if (element is IPropertyInitializer propertyInitializer)
        {
            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "Insert arguments"))
            {
                var factory = CSharpElementFactory.GetInstance(propertyInitializer);
                var expression = createExpressionFn(Info, factory);
                propertyInitializer.SetExpression(expression);
            }
        }
        else if (element is IAssignmentExpression assignmentExpression)
        {
            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "Insert arguments"))
            {
                var factory = CSharpElementFactory.GetInstance(assignmentExpression);
                var expression = createExpressionFn(Info, factory);
                assignmentExpression.SetSource(expression);
            }
        }
        else if (element is IExpressionInitializer expressionInitializer)
        {
            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "Insert arguments"))
            {
                var factory = CSharpElementFactory.GetInstance(expressionInitializer);
                var expression = createExpressionFn(Info, factory);
                expressionInitializer.SetValue(expression);
            }
        }
        else
        {
            throw new Exception("Scenario not supported, please report this on github: https://github.com/Socolin/NSubstituteComplete/issues");
        }
    }

    private ITreeNode GetTargetElementToReplace(ISolution solution, ITextControl textControl)
    {
        var element = TextControlToPsi.GetElement<ITreeNode>(solution, textControl);
        while (element != null)
        {
            if (element is ICSharpArgument)
                return element;
            if (element is IPropertyInitializer)
                return element;
            if (element is IAssignmentExpression)
                return element;
            if (element is IExpressionInitializer)
                return element;
            element = element.Parent;
        }

        return null;
    }
}
