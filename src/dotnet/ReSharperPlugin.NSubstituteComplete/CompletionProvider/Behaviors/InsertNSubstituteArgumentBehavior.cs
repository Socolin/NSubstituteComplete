using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors
{
    public class InsertNSubstituteArgumentBehavior : TextualBehavior<NSubstituteArgumentInformation>
    {
        public InsertNSubstituteArgumentBehavior(NSubstituteArgumentInformation info)
            : base(info)
        {
        }

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
            var argumentExpression = TextControlToPsi.GetElement<ICSharpArgument>(solution, textControl);
            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "Insert arguments"))
            {
                var factory = CSharpElementFactory.GetInstance(argumentExpression);
                var newArgument = factory.CreateArgument(ParameterKind.VALUE, factory.CreateExpression($"Arg.{Info.ArgSuffix}<$0>()", Info.Type));
                argumentExpression.ReplaceBy(newArgument);
            }
        }
    }
}
