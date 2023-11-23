using System;
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
using ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider
{
    [Language(typeof(CSharpLanguage))]
    public class SubstituteForCompletionProvider : CSharpItemsProviderBase<CSharpCodeCompletionContext>
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

            if (!(context.TerminatedContext.TreeNode?.GetContainingFile() is ICSharpFile cSharpFile))
                return false;

            if (!cSharpFile.Imports.Any(i => i is IUsingSymbolDirective usingSymbolDirective && usingSymbolDirective.ImportedSymbolName.QualifiedName == "NSubstitute"))
                if (cSharpFile.GetProject()?.GetAllReferencedAssemblies().Any(x => x.Name == "NSubstitute") != true)
                    return false;

            foreach (var expectedType in context.ExpectedTypesContext.ExpectedITypes)
            {
                if (expectedType.DeclaredType?.GetTypeElement() is ITypeMember typeElement)
                {
                    if (typeElement.IsSealed)
                        continue;

                    AddSubstituteForLookupItem(context, collector, expectedType);
                }
            }

            return true;
        }

        private static void AddSubstituteForLookupItem(CSharpCodeCompletionContext context, IItemsCollector collector, ExpectedTypeCompletionContextBase.ExpectedIType expectedType)
        {
            if (context.IsQualified && !LeftPartStartsWith(context, "Substitute", "For<"))
                return;
            var typeName = expectedType.Type.GetPresentableName(context.Language);
            var text = context.IsQualified ? $"For<{typeName}>()" : $"Substitute.For<{typeName}>()";
            var lookupItem = CreateArgumentLookupItem(context, "For", '\0', text, typeName, expectedType.DeclaredType);
            if (lookupItem != null)
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

        private static LookupItem<NSubstituteArgumentInformation> CreateArgumentLookupItem(
            CSharpCodeCompletionContext context,
            string argSuffix,
            char typeFirstLetter,
            string text,
            string typename,
            IType type
        )
        {
            var info = new NSubstituteArgumentInformation(text, text, type, argSuffix, typeFirstLetter)
            {
                Ranges = context.CompletionRanges,
                IsDynamic = false,
                TypeName = typename,
            };

            var lookupItem = LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(static p => new TextPresentation<NSubstituteArgumentInformation>(p.Info, p.Info.TypeName, true))
                .WithBehavior(static b => new InsertNSubstituteArgumentBehavior(b.Info,
                    (information, factory) =>
                    {
                        var substituteClass = TypeFactory.CreateTypeByCLRName("NSubstitute.Substitute", information.Type.Module);
                        return factory.CreateExpression("$0.For<$1>()", substituteClass, information.Type);
                    }))
                .WithMatcher(static m => new TextualMatcher<TextualInfo>(m.Info));

            lookupItem.WithPresentation(p => new TextPresentation<NSubstituteArgumentInformation>(p.Info, null, true, PsiSymbolsThemedIcons.Method.Id));
            lookupItem.WithHighSelectionPriority();

            return lookupItem;
        }
    }
}
