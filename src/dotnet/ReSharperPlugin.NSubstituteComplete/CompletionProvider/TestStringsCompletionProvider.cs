using System.Text;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider
{
    [Language(typeof(CSharpLanguage))]
    public class TestStringsCompletionProvider : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            return context.BasicContext.CodeCompletionType == CodeCompletionType.BasicCompletion || context.BasicContext.CodeCompletionType == CodeCompletionType.SmartCompletion;
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            if (!(context.TerminatedContext.TreeNode is CSharpGenericToken cSharpGenericToken)
                || cSharpGenericToken.NodeType != CSharpTokenType.STRING_LITERAL_REGULAR)
                return false;

            if (cSharpGenericToken.GetContainingTypeDeclaration()?.NameIdentifier.Name.ToLowerInvariant().Contains("tests") != true)
                return false;

            if (cSharpGenericToken.Parent?.Parent is INamedMemberInitializer namedMemberInitializer)
                return AutoCompleteNamedMember(context, collector, namedMemberInitializer);

            if (cSharpGenericToken.Parent?.Parent is IDeclaration declaration)
                return AutoCompleteDeclaration(context, collector, declaration);

            if (cSharpGenericToken.Parent?.Parent is IExpressionInitializer expressionInitializer)
                return AutoCompleteExpressionInitializer(context, collector, expressionInitializer);

            return false;
        }

        private bool AutoCompleteDeclaration(CSharpCodeCompletionContext context, IItemsCollector collector, IDeclaration declaration)
        {
            AddLookupItem(context, collector, declaration.DeclaredName);
            return true;
        }

        private bool AutoCompleteExpressionInitializer(CSharpCodeCompletionContext context, IItemsCollector collector, IExpressionInitializer expressionInitializer)
        {
            if (!(expressionInitializer.Parent is IDeclaration declaration))
                return false;
            AddLookupItem(context, collector, declaration.DeclaredName);
            return true;
        }

        private bool AutoCompleteNamedMember(CSharpCodeCompletionContext context, IItemsCollector collector, INamedMemberInitializer propertyInitializer)
        {
            var typeName = (propertyInitializer.GetConstructedType() as DeclaredTypeBase)?.GetClrName().ShortName;

            AddLookupItem(context, collector, propertyInitializer.MemberName);
            if (typeName != null)
                AddLookupItem(context, collector, typeName + "-" + propertyInitializer.MemberName);
            return true;
        }

        private static void AddLookupItem(CSharpCodeCompletionContext context, IItemsCollector collector, string name)
        {
            var autocompleteText = new StringBuilder();
            autocompleteText.Append('"').Append("some-").Append(TextUtil.ToKebabCase(name)).Append('"');
            var lookupItem = CSharpLookupItemFactory.Instance.CreateTextLookupItem(context.CompletionRanges, autocompleteText.ToString());
            collector.Add(lookupItem);
        }
    }
}