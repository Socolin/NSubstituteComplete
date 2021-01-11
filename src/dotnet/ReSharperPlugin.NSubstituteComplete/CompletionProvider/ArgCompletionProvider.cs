using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems.Behavior;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules;
using JetBrains.ReSharper.InplaceRefactorings.CutCopyPaste;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
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
                AddArgLookupItem(context, collector, expectedType);
                AddIsLookupItem(context, collector, expectedType);
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

                var declaredElements = method.TypeParameters.Select(x => x.GetDeclarations().Select(d => d.DeclaredElement)).SelectMany(element => element);
                var parameter = method.Parameters.Select(x => $"Arg.Any<{x.Type.GetPresentableName(CSharpLanguage.Instance)}>()");
                var completionText = string.Join(", ", parameter);
                var lookupItem = CreateTextLookupItem(context, completionText, declaredElements, method.Parameters.Select(x => x.Type()));
                if (lookupItem != null)
                    collector.Add(lookupItem);
            }

            return true;
        }

        private static void AddArgLookupItem(CSharpCodeCompletionContext context, IItemsCollector collector, ExpectedTypeCompletionContextBase.ExpectedIType expectedType)
        {
            if (context.IsQualified && !LeftPartStartsWith(context, "Arg.", "Any<"))
                return;
            var typeName = expectedType.Type.GetPresentableName(CSharpLanguage.Instance);
            var text = context.IsQualified ? $"Any<{typeName}>()" : $"Arg.Any<{typeName}>()";
            var lookupItem = CreateLookupItem(context, text, typeName, expectedType.DeclaredType);
            if (lookupItem != null)
                collector.Add(lookupItem);
        }

        private static void AddIsLookupItem(CSharpCodeCompletionContext context, IItemsCollector collector, ExpectedTypeCompletionContextBase.ExpectedIType expectedType)
        {
            if (context.IsQualified && !LeftPartStartsWith(context, "Arg.", "Is<"))
                return;

            var typeName = expectedType.Type.GetPresentableName(CSharpLanguage.Instance);
            var firstLetter = typeName.First().ToLowerFast();
            var text = context.IsQualified ? $"Is<{typeName}>({firstLetter} => )" : $"Arg.Is<{typeName}>({firstLetter} => )";

            var lookupItem = CreateLookupItem(context, text, typeName, expectedType.DeclaredType);
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

        private static LookupItem<DeclaredElementInfo> CreateLookupItem(CSharpCodeCompletionContext context, string text, string typename, IDeclaredType declaredElement)
        {
            var typeElement = declaredElement.GetTypeElement();
            if (typeElement == null)
                return null;
            var lookupItem = LookupItemFactory.CreateLookupItem(new DeclaredElementInfo(text, typeElement, context.Language, context.BasicContext.LookupItemsOwner, DefaultElementPointerFactory.Instance))
                .WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<DeclaredElementInfo>(_.Info, typename, true))
                .WithBehavior(_ => (ILookupItemBehavior) new ReferenceTypesTextualBehavior(new[] {declaredElement}, context.NodeInFile.GetContainingFile(), _.Info))
                .WithMatcher(_ => (ILookupItemMatcher) new TextualMatcher<TextualInfo>(_.Info));
            lookupItem.WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<DeclaredElementInfo>(_.Info, typename, true, PsiSymbolsThemedIcons.Method.Id));
            lookupItem.WithHighSelectionPriority();
            lookupItem.SetBind(true);
            lookupItem.SetRanges(context.CompletionRanges);

            return lookupItem;
        }

        private static LookupItem<DeclaredElementInfo> CreateTextLookupItem(CSharpCodeCompletionContext context, string text, IEnumerable<IDeclaredElement> declaredElements, IEnumerable<IType> types)
        {
            var lookupItem = LookupItemFactory.CreateLookupItem(new DeclaredElementInfo(text, declaredElements, context.Language, context.BasicContext.LookupItemsOwner, DefaultElementPointerFactory.Instance))
                .WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<DeclaredElementInfo>(_.Info, null, true))
                .WithBehavior(_ => (ILookupItemBehavior) new ReferenceTypesTextualBehavior(types.ToArray(), context.NodeInFile.GetContainingFile(), _.Info))
                .WithMatcher(_ => (ILookupItemMatcher) new TextualMatcher<TextualInfo>(_.Info));
            lookupItem.WithPresentation(_ => (ILookupItemPresentation) new TextPresentation<DeclaredElementInfo>(_.Info, null, true, PsiSymbolsThemedIcons.Method.Id));
            lookupItem.WithHighSelectionPriority();
            lookupItem.SetBind(true);
            lookupItem.SetRanges(context.CompletionRanges);

            return lookupItem;
        }

        private class ReferenceTypesTextualBehavior : TextualBehavior<DeclaredElementInfo>
        {
            private readonly IType[] _types;
            private readonly IFile _file;

            public ReferenceTypesTextualBehavior(IType[] types, IFile file, DeclaredElementInfo info) : base(info)
            {
                _types = types;
                _file = file;
            }

            protected override void OnAfterComplete(ITextControl textControl, LookupItemInsertType insertType, ref DocumentRange nameRange, ref DocumentRange decorationRange, TailType tailType, ref Suffix suffix, ref IRangeMarker caretPositionRangeMarker, ref bool keepCaretStill)
            {
                if (_file is ICSharpFile cSharpFile)
                {
                    var missingNamespaces = _types
                        .OfType<DeclaredTypeBase>()
                        .Select(declaredType => declaredType.GetTypeElement()?.GetContainingNamespace())
                        .Distinct()
                        .Where(ns => !UsingUtil.CheckNamespaceAlreadyImported(cSharpFile, ns))
                        .ToList();
                    if (missingNamespaces.Count > 0)
                    {
                        Info.Owner.Services.PsiServices.Files.CommitAllDocuments();
                        Info.Owner.Services.PsiServices.Transactions.Execute("Add missing using", () =>
                        {
                            foreach (var missingNamespace in missingNamespaces)
                                UsingUtil.AddImportTo(cSharpFile, missingNamespace);
                        });
                    }
                }

                base.OnAfterComplete(textControl, insertType, ref nameRange, ref decorationRange, tailType, ref suffix, ref caretPositionRangeMarker, ref keepCaretStill);
            }

            // FIXME: To investigate: This seems to be the "good way" for AddImportTo but I need to figure out what are IReference and ReferenceData
            private static void BindRefs(
                [NotNull] IEnumerable<Tuple<IReference, IDeclaredElement, ReferenceData>> tuples, [NotNull] IPsiServices psiServices
            )
            {
                bool anyChange;
                var refsToBind = tuples.ToList();
                do
                {
                    var refsLeft = new List<Tuple<IReference, IDeclaredElement, ReferenceData>>();
                    anyChange = false;
                    foreach (var tuple in refsToBind)
                    {
                        var reference = tuple.Item1;
                        var target = tuple.Item2;
                        if (reference.IsValid() && target.IsValid())
                        {
                            using (var transactionCookie = new PsiTransactionCookie(psiServices, DefaultAction.Commit, null))
                            {
                                var referenceData = tuple.Item3;
                                if (referenceData.BindReference(reference, target)) anyChange = true;
                                else
                                {
                                    transactionCookie.Rollback();
                                    Assertion.Assert(reference.IsValid(), "@ref.IsValid()");
                                    refsLeft.Add(tuple);
                                }
                            }
                        }
                    }

                    refsToBind = refsLeft;
                } while (anyChange);
            }
        }
    }
}