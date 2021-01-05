using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.NSubstituteComplete.ContextActions
{
    [ContextAction(Group = "C#", Name = "Fill all parameters with Mock", Description = "Auto generate mocks for arguments", Priority = short.MinValue + 5)]
    public class FillAllParamWithMockContextAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider _dataProvider;
        private readonly IObjectCreationExpression _objectCreationExpression;
        private IConstructor _constructor;
        private IClassDeclaration _classDeclaration;

        public FillAllParamWithMockContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
            _objectCreationExpression = _dataProvider.GetSelectedElement<IObjectCreationExpression>(false, false);
        }

        public override string Text => "Fill all parameters with Mock";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            if (_objectCreationExpression == null)
                return false;

            if (_objectCreationExpression.ArgumentList.Arguments.Count > 0)
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

            _classDeclaration = classDeclaration;

            return true;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var substituteClass = TypeFactory.CreateTypeByCLRName("NSubstitute.Substitute", _classDeclaration.GetPsiModule());
            var block = _dataProvider.GetSelectedElement<IBlock>();
            var arguments = new LocalList<ICSharpArgument>();
            foreach (var parameter in _constructor.Parameters)
            {
                if (!(parameter.Type is DeclaredTypeBase declaredTypeBase))
                    continue;
                var options = new SuggestionOptions(defaultName: declaredTypeBase.GetClrName().ShortName);
                var elementFactory = _dataProvider.ElementFactory;
                var fieldDeclaration = elementFactory.CreateTypeMemberDeclaration("private $0 $1;", parameter.Type, declaredTypeBase.GetClrName().ShortName);
                var fieldName = _dataProvider.PsiServices.Naming.Suggestion.GetDerivedName(fieldDeclaration.DeclaredElement, NamedElementKinds.PrivateInstanceFields, ScopeKind.Common, _classDeclaration.Language, options, _dataProvider.SourceFile);
                fieldDeclaration.SetName(fieldName);
                _classDeclaration.AddClassMemberDeclaration((IClassMemberDeclaration) fieldDeclaration);

                var initializeMockStatement = elementFactory.CreateStatement("$0 = $1.For<$2>();", fieldName, substituteClass, parameter.Type);
                block.AddStatementBefore(initializeMockStatement, _objectCreationExpression.GetContainingStatement());

                var argument = elementFactory.CreateArgument(ParameterKind.VALUE, elementFactory.CreateExpression("$0", fieldName));
                arguments.Add(argument);
            }

            arguments.Reverse();
            foreach (var argument in arguments)
            {
                _objectCreationExpression.AddArgumentAfter(argument, null);
            }

            return textControl =>
            {

                /*
                textControl.Document.InsertText(_objectCreationExpression.ArgumentList.GetDocumentStartOffset(), constructorParameters.ToString());
                */

                /*
                var offset = _classDeclaration.FieldDeclarations.LastOrDefault()?.GetDocumentStartOffset() ?? _classDeclaration.Body.GetDocumentStartOffset();
                foreach (var fieldDeclaration in fieldsDeclaration)
                {
                  //  _classDeclaration.AddClassMemberDeclaration((IClassMemberDeclaration)fieldsDeclaration);

                    // textControl.Document.InsertText(offset, fieldDeclaration.GetText());
                }*/
            };
        }
    }
}