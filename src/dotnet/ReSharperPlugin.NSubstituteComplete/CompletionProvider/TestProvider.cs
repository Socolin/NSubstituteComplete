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

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider
{
    [Language(typeof(CSharpLanguage))]
    public class TestProvider : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
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

            if (cSharpGenericToken.Parent?.Parent?.Parent is ILocalVariableDeclaration localVariableDeclaration)
                return AutoCompleteLocalVariable(context, collector, localVariableDeclaration);

            if (cSharpGenericToken.Parent?.Parent is IPropertyInitializer propertyInitializer)
                return AutoCompletePropertyInitializer(context, collector, propertyInitializer);

            return false;
        }

        private static bool AutoCompleteLocalVariable(CSharpCodeCompletionContext context, IItemsCollector collector, ILocalVariableDeclaration localVariableDeclaration)
        {
            AddLookupItem(context, collector, localVariableDeclaration.DeclaredName);
            return true;
        }

        private bool AutoCompletePropertyInitializer(CSharpCodeCompletionContext context, IItemsCollector collector, IPropertyInitializer propertyInitializer)
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