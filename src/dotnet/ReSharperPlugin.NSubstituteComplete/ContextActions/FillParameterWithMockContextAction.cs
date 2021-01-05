using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.NSubstituteComplete.ContextActions
{
    [ContextAction(Group = "C#", Name = "Fill the parameter with Mock", Description = "Auto generate mock for this argument", Priority = short.MinValue + 1)]
    public class FillParameterWithMockContextAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider _dataProvider;
        private readonly IObjectCreationExpression _objectCreationExpression;
        private IConstructor _constructor;
        private IClassDeclaration _classDeclaration;
        private (int argumentIndex, ICSharpArgument argument, ICSharpArgument previousArgument) _selectedArgument;

        public FillParameterWithMockContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
            _objectCreationExpression = _dataProvider.GetSelectedElement<IObjectCreationExpression>(false, false);
        }

        public override string Text => "Fill the current parameter with Mock";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            if (_objectCreationExpression == null)
                return false;

            if (!(_objectCreationExpression.TypeReference?.Resolve().DeclaredElement is IClass c))
                return false;

            _constructor = c.Constructors.Where(x => !x.IsParameterless).OrderByDescending(con => con.Parameters.Count).FirstOrDefault();
            if (_constructor == null)
                return false;

            if (!(_objectCreationExpression.GetContainingTypeDeclaration() is IClassDeclaration classDeclaration))
                return false;

            if (!classDeclaration.CLRName.ToLowerInvariant().Contains("tests"))
                return false;

            _selectedArgument = GetSelectedArgument();

            if (_selectedArgument.argumentIndex == -1)
                return false;

            if (_selectedArgument.argument != null && _selectedArgument.argument.Kind != ParameterKind.UNKNOWN)
                return false;

            _classDeclaration = classDeclaration;

            return true;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var substituteClass = TypeFactory.CreateTypeByCLRName("NSubstitute.Substitute", _classDeclaration.GetPsiModule());
            var block = _dataProvider.GetSelectedElement<IBlock>();

            var parameter = _constructor.Parameters[_selectedArgument.argumentIndex];
            if (!(parameter.Type is DeclaredTypeBase declaredTypeBase)) // FIXME. else ?
                return control => { };

            var options = new SuggestionOptions(defaultName: declaredTypeBase.GetClrName().ShortName);
            var fieldDeclaration = _dataProvider.ElementFactory.CreateTypeMemberDeclaration("private $0 $1;", parameter.Type, declaredTypeBase.GetClrName().ShortName);
            var fieldName = _dataProvider.PsiServices.Naming.Suggestion.GetDerivedName(fieldDeclaration.DeclaredElement, NamedElementKinds.PrivateInstanceFields, ScopeKind.Common, _classDeclaration.Language, options, _dataProvider.SourceFile);
            fieldDeclaration.SetName(fieldName);
            _classDeclaration.AddClassMemberDeclaration((IClassMemberDeclaration) fieldDeclaration);

            var initializeMockStatement = _dataProvider.ElementFactory.CreateStatement("$0 = $1.For<$2>();", fieldName, substituteClass, parameter.Type);
            block.AddStatementBefore(initializeMockStatement, _objectCreationExpression.GetContainingStatement());

            var argumentExpression = _dataProvider.ElementFactory.CreateArgument(ParameterKind.VALUE, _dataProvider.ElementFactory.CreateExpression("$0", fieldName));

            var whitespaceNode = _dataProvider.GetSelectedElement<IWhitespaceNode>();
            if (whitespaceNode != null)
                using (WriteLockCookie.Create())
                    LowLevelModificationUtil.DeleteChild(whitespaceNode);

            _objectCreationExpression.AddArgumentAfter(argumentExpression, _selectedArgument.previousArgument);
            if (_selectedArgument.argument != null)
                _objectCreationExpression.RemoveArgument(_selectedArgument.argument);

            return textControl => { };
        }

        private (int argumentIndex, ICSharpArgument argument, ICSharpArgument previousArgument) GetSelectedArgument()
        {
            if (_objectCreationExpression.ArgumentList.Arguments.IsEmpty)
                return (0, null, null);

            ICSharpArgument argument = null;
            for (var argumentIndex = 0; argumentIndex < _objectCreationExpression.ArgumentList.Arguments.Count; argumentIndex++)
            {
                var previousArgument = argument;
                argument = _objectCreationExpression.ArgumentList.Arguments[argumentIndex];
                var trailingWhiteSpaces = argument.NextTokens().TakeWhile(x => x.IsWhitespaceToken()).Select(x => x.GetTextLength()).Sum();
                var argumentDocumentRange = argument.GetDocumentRange().ExtendRight(trailingWhiteSpaces);

                if (argumentDocumentRange.IntersectsOrContacts(_dataProvider.DocumentSelection))
                    return (argumentIndex, argument, previousArgument);
            }

            return (-1, null, null);
        }
    }
}