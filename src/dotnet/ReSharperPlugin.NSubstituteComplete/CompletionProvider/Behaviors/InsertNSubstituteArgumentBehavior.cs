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

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors
{
    public class InsertNSubstituteArgumentBehavior : TextualBehavior<NSubstituteArgumentInformation>
    {
        private readonly Func<NSubstituteArgumentInformation, CSharpElementFactory, ICSharpArgument> _createArgumentFn;

        public InsertNSubstituteArgumentBehavior(NSubstituteArgumentInformation info, Func<NSubstituteArgumentInformation, CSharpElementFactory, ICSharpArgument> createArgumentFn)
            : base(info)
        {
            _createArgumentFn = createArgumentFn;
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
            ICSharpArgument addedArgument;
            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "Insert arguments"))
            {
                var factory = CSharpElementFactory.GetInstance(argumentExpression);
                var newArgument = _createArgumentFn(Info, factory);
                addedArgument = argumentExpression.ReplaceBy(newArgument);
            }
            textControl.Caret.MoveTo(addedArgument.GetDocumentEndOffset().Shift(Info.InsertCaretOffset), CaretVisualPlacement.Generic);
        }
    }
}
