using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider
{
    [Language(typeof(CSharpLanguage))]
    public class ArgCompletionProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            var codeCompletionType = context.BasicContext.CodeCompletionType;

            return codeCompletionType == CodeCompletionType.BasicCompletion || codeCompletionType == CodeCompletionType.SmartCompletion;
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            if (context.ExpectedTypesContext == null)
                return false;

            var identifier = context.TerminatedContext.TreeNode as IIdentifier;
            var mockedMethodArgument = (identifier?.Parent as IReferenceExpression)?.Parent as ICSharpArgument;
            var mockedMethodInvocationExpression = (mockedMethodArgument?.Parent as IArgumentList)?.Parent as IInvocationExpression;

            if (mockedMethodInvocationExpression == null)
                return false;

            if (!(context.TerminatedContext.TreeNode?.GetContainingFile() is ICSharpFile cSharpFile))
                return false;
            if (!cSharpFile.Imports.Any(i => i.ImportedSymbolName.QualifiedName == "NSubstitute"))
                return false;

            foreach (var expectedType in context.ExpectedTypesContext.ExpectedITypes)
            {
                AddArgLookupItem(context, collector, expectedType.Type);
                AddIsLookupItem(context, collector, expectedType.Type);
            }

            var argumentIndex = mockedMethodArgument.IndexOf();
            if (argumentIndex != 0)
                return false;

            if (context.IsQualified)
                return false;

            var candidates = mockedMethodInvocationExpression.InvocationExpressionReference.GetCandidates();
            foreach (var candidate in candidates)
            {
                if (!(candidate.GetDeclaredElement() is IMethod method))
                    continue;
                if (method.Parameters.Count <= 1)
                    continue;

                var parameter = method.Parameters.Select(x => $"Arg.Any<{x.Type.GetPresentableName(CSharpLanguage.Instance)}>()");
                var completionText = string.Join(", ", parameter);
                collector.Add(CreateTextLookupItem(context.CompletionRanges, completionText, null));
            }

            return true;
        }

        private static void AddArgLookupItem(CSharpCodeCompletionContext context, IItemsCollector collector, IType type)
        {
            if (context.IsQualified && LeftPartIsArg(context))
                return;
            var typeName = type.GetPresentableName(CSharpLanguage.Instance);
            var text = context.IsQualified ? $"Any<{typeName}>()" : $"Arg.Any<{typeName}>()";
            collector.Add(CreateLookupItem(context, text, typeName, type));
        }

        private static void AddIsLookupItem(CSharpCodeCompletionContext context, IItemsCollector collector, IType type)
        {
            if (context.IsQualified && LeftPartIsArg(context))
                return;

            var typeName = type.GetPresentableName(CSharpLanguage.Instance);
            var firstLetter = typeName.First().ToLowerFast();
            var text = context.IsQualified ? $"Is<{typeName}>({firstLetter} => )" : $"Arg.Is<{typeName}>({firstLetter} => )";

            var lookupItem = CreateLookupItem(context, text, typeName, type);
            lookupItem.SetInsertCaretOffset(-1);
            collector.Add(lookupItem);
        }

        private static bool LeftPartIsArg(CSharpCodeCompletionContext context)
        {
            var leftPartType = (context.NodeInFile.PrevSibling as IReferenceExpression)?.Reference.Resolve().DeclaredElement?.ShortName;
            if (leftPartType != "Arg")
                return true;
            return false;
        }

        private static LookupItem<TypeInfo> CreateLookupItem(CSharpCodeCompletionContext context, string text, string typename, IType type)
        {
            var lookupItem = LookupItemFactory.CreateLookupItem(new TypeInfo(text, type, CSharpLanguage.Instance, context.BasicContext.LookupItemsOwner))
                .WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<TypeInfo>(_.Info, typename, true))
                .WithBehavior(_ => (ILookupItemBehavior) new TextualBehavior<TypeInfo>(_.Info))
                .WithMatcher(_ => (ILookupItemMatcher) new TextualMatcher<TextualInfo>(_.Info));
            lookupItem.WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<TypeInfo>(_.Info, typename, true, PsiSymbolsThemedIcons.Method.Id));
            lookupItem.WithHighSelectionPriority();
            lookupItem.SetBind(true);
            lookupItem.SetRanges(context.CompletionRanges);

            return lookupItem;
        }

        private static LookupItem<TextualInfo> CreateTextLookupItem(TextLookupRanges completionRanges, string text, string type)
        {
            var lookupItem = CSharpLookupItemFactory.Instance.CreateTextLookupItem(completionRanges, text, type, false, false);
            lookupItem.WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<TextualInfo>(_.Info, type, true, PsiSymbolsThemedIcons.Method.Id));
            lookupItem.WithHighSelectionPriority();

            return lookupItem;
        }
    }
}