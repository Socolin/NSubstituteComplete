using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors;

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
                if (cSharpFile.GetProject()?.GetAllReferencedAssemblies().Any(x => x.Name == "NSubstitute") != true)
                    return false;

            foreach (var expectedType in context.ExpectedTypesContext.ExpectedITypes)
            {
                AddArgLookupItem(context, collector, expectedType);
                AddIsLookupItem(context, collector, expectedType);
            }

            var argumentIndex = mockedMethodArgument.IndexOf();
            if (argumentIndex != 0)
                return false;

            if (context.IsQualified && !LeftPartStartsWith(context, "Arg.", "Any<"))
                return false;

            var candidates = mockedMethodInvocationExpression.InvocationExpressionReference.GetCandidates();
            foreach (var candidate in candidates)
            {
                if (!(candidate.GetDeclaredElement() is IMethod method))
                    continue;
                if (method.Parameters.Count <= 1)
                    continue;

                var parameter = method.Parameters.Select(x => $"Arg.Any<{x.Type.GetPresentableName(context.Language)}>()");
                var completionText = string.Join(", ", parameter);
                var lookupItem = CreateMultipleArgumentsLookupItem(context, completionText, method.Parameters.Select(p => p.Type));
                if (lookupItem != null)
                    collector.Add(lookupItem);
            }

            return true;
        }

        private static void AddArgLookupItem(CSharpCodeCompletionContext context, IItemsCollector collector, ExpectedTypeCompletionContextBase.ExpectedIType expectedType)
        {
            if (context.IsQualified && !LeftPartStartsWith(context, "Arg.", "Any<"))
                return;
            var typeName = expectedType.Type.GetPresentableName(context.Language);
            var text = context.IsQualified ? $"Any<{typeName}>()" : $"Arg.Any<{typeName}>()";
            var lookupItem = CreateArgumentLookupItem(context, "Any", '\0', text, typeName, expectedType.DeclaredType ?? expectedType.Type);
            if (lookupItem != null)
                collector.Add(lookupItem);
        }

        private static void AddIsLookupItem(CSharpCodeCompletionContext context, IItemsCollector collector, ExpectedTypeCompletionContextBase.ExpectedIType expectedType)
        {
            if (context.IsQualified && !LeftPartStartsWith(context, "Arg.", "Is<"))
                return;

            var typeName = expectedType.Type.GetPresentableName(context.Language);
            var firstLetter = typeName.First().ToLowerFast();
            var text = context.IsQualified ? $"Is<{typeName}>({firstLetter} => )" : $"Arg.Is<{typeName}>({firstLetter} => )";

            var lookupItem = CreateArgumentLookupItem(context, "Is", firstLetter, text, typeName, expectedType.DeclaredType ?? expectedType.Type);
            lookupItem.SetInsertCaretOffset(-1);
            collector.Add(lookupItem);
        }

        private static bool LeftPartStartsWith(CSharpCodeCompletionContext context, string expectedStats, string expectedOptionalPart)
        {
            var leftPart = (context.NodeInFile.Parent as IReferenceExpression)?.GetText();
            if (leftPart?.StartsWith(expectedStats, StringComparison.InvariantCultureIgnoreCase) != true)
                return false;

            var optionalPart = leftPart.Substring(expectedStats.Length);
            if (string.IsNullOrEmpty(optionalPart))
                return true;

            if (expectedOptionalPart.StartsWith(optionalPart, StringComparison.InvariantCultureIgnoreCase))
                return true;
            return false;
        }

        private static LookupItem<NSubstituteArgumentInformation> CreateArgumentLookupItem(CSharpCodeCompletionContext context, string argSuffix, char typeFirstLetter, string text, string typename, IType type)
        {
            var info = new NSubstituteArgumentInformation(text, text, type, argSuffix, typeFirstLetter)
            {
                Ranges = context.CompletionRanges,
                IsDynamic = false
            };

            var lookupItem = LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<NSubstituteArgumentInformation>(_.Info, typename, true))
                .WithBehavior(_ => (ILookupItemBehavior) new InsertNSubstituteArgumentBehavior(_.Info,
                    (information, factory) =>
                    {
                        var argClass = TypeFactory.CreateTypeByCLRName("NSubstitute.Arg", information.Type.Module);

                        if (information.ArgSuffix == "Is")
                            return factory.CreateExpression($"$0.{information.ArgSuffix}<$1>({information.TypeFirstLetter} => )", argClass, information.Type);
                        return factory.CreateExpression($"$0.{information.ArgSuffix}<$1>()", argClass, information.Type);
                    }))
                .WithMatcher(_ => (ILookupItemMatcher) new TextualMatcher<TextualInfo>(_.Info));

            lookupItem.WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<NSubstituteArgumentInformation>(_.Info, null, true, PsiSymbolsThemedIcons.Method.Id));
            lookupItem.WithHighSelectionPriority();
            if (argSuffix == "Is")
            {
                lookupItem.SetInsertCaretOffset(-1);
                lookupItem.SetReplaceCaretOffset(-1);
            }

            return lookupItem;
        }

        private static LookupItem<NSubstituteArgumentsInformation> CreateMultipleArgumentsLookupItem(CSharpCodeCompletionContext context, string text, IEnumerable<IType> types)
        {
            var info = new NSubstituteArgumentsInformation(text, text, types, "Any")
            {
                Ranges = context.CompletionRanges,
                IsDynamic = false
            };

            var lookupItem = LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<NSubstituteArgumentsInformation>(_.Info, null, true))
                .WithBehavior(_ => (ILookupItemBehavior) new InsertNSubstituteArgumentsBehavior(_.Info))
                .WithMatcher(_ => (ILookupItemMatcher) new TextualMatcher<TextualInfo>(_.Info));

            lookupItem.WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<NSubstituteArgumentsInformation>(_.Info, null, true, PsiSymbolsThemedIcons.Method.Id));
            lookupItem.WithHighSelectionPriority();

            return lookupItem;
        }
    }
}
