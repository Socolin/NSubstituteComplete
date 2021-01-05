using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.NSubstituteComplete.QuickFixes
{
    [QuickFix]
    public class FillMissingConstructorArgumentsWithMock : QuickFixBase
    {
        private readonly IncorrectArgumentNumberError _incorrectArgumentNumberError;
        private readonly MultipleResolveCandidatesError _multipleResolveCandidatesError;
        private readonly IncorrectArgumentsError _incorrectArgumentsError;
        private readonly IObjectCreationExpression _objectCreationExpression;
        private IClassDeclaration _classDeclaration;

        public FillMissingConstructorArgumentsWithMock(MultipleResolveCandidatesError error)
        {
            _multipleResolveCandidatesError = error;
            _objectCreationExpression = (error.Reference as ICSharpInvocationReference)?.Invocation as IObjectCreationExpression;
        }

        public FillMissingConstructorArgumentsWithMock(IncorrectArgumentsError error)
        {
            _incorrectArgumentsError = error;
            _objectCreationExpression = (error.Reference as ICSharpInvocationReference)?.Invocation as IObjectCreationExpression;
        }

        public FillMissingConstructorArgumentsWithMock(IncorrectArgumentNumberError incorrectArgumentNumberError)
        {
            _incorrectArgumentNumberError = incorrectArgumentNumberError;
            _objectCreationExpression = (incorrectArgumentNumberError.Reference as ICSharpInvocationReference)?.Invocation as IObjectCreationExpression;
        }

        public override string Text => "Fill missing arguments with NSubstitute mocks";

        public override bool IsAvailable(IUserDataHolder cache)
        {
            if (_objectCreationExpression == null)
                return false;

            if (!GetCandidates().Any())
                return false;

            if (!(_objectCreationExpression.GetContainingTypeDeclaration() is IClassDeclaration classDeclaration))
                return false;

            if (!classDeclaration.CLRName.ToLowerInvariant().Contains("tests"))
                return false;

            _classDeclaration = classDeclaration;

            return true;
        }

        private IEnumerable<ISymbolInfo> GetCandidates()
        {
            if (_incorrectArgumentNumberError != null)
                return ((ICSharpInvocationReference) _incorrectArgumentNumberError.Reference).GetCandidates();
            if (_incorrectArgumentsError != null)
                return ((ICSharpInvocationReference) _incorrectArgumentsError.Reference).GetCandidates();
            return ((ICSharpInvocationReference) _multipleResolveCandidatesError.Reference).GetCandidates();
        }

        private ITreeNode GetTreeNode()
        {
            return _incorrectArgumentNumberError?.Reference.GetTreeNode()
                   ?? _incorrectArgumentsError?.Reference.GetTreeNode()
                   ?? _multipleResolveCandidatesError.Reference.GetTreeNode();
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var targetConstructor = GetCandidates().Select(x => x.GetDeclaredElement()).Cast<IConstructor>().OrderByDescending(con => con.Parameters.Count).FirstOrDefault();
            if (targetConstructor == null)
                return _ => { };

            var substituteClass = TypeFactory.CreateTypeByCLRName("NSubstitute.Substitute", _classDeclaration.GetPsiModule());
            var block = _objectCreationExpression.GetContainingNode<IBlock>();
            if (block == null)
                return _ => { };

            var treeNode = GetTreeNode();
            var elementFactory = CSharpElementFactory.GetInstance(treeNode);
            var psiServices = treeNode.GetPsiServices();
            var psiSourceFile = treeNode.GetSourceFile();

            var arguments = new LocalList<ICSharpArgument>();
            foreach (var parameter in targetConstructor.Parameters)
            {
                if (!(parameter.Type is DeclaredTypeBase declaredTypeBase))
                    continue;

                if (declaredTypeBase.GetClrName().FullName == "System.String")
                {
                    var argument = elementFactory.CreateArgument(ParameterKind.VALUE, elementFactory.CreateExpression("$0", $@"""some-{TextUtil.ToKebabCase(parameter.ShortName)}"""));
                    arguments.Add(argument);
                }
                else
                {
                    var options = new SuggestionOptions(defaultName: declaredTypeBase.GetClrName().ShortName);
                    var fieldDeclaration = elementFactory.CreateTypeMemberDeclaration("private $0 $1;", parameter.Type, declaredTypeBase.GetClrName().ShortName);

                    var fieldName = psiServices.Naming.Suggestion.GetDerivedName(fieldDeclaration.DeclaredElement, NamedElementKinds.PrivateInstanceFields, ScopeKind.Common, _classDeclaration.Language, options, psiSourceFile);

                    var existingArgument = GetExistingArgument(parameter, fieldName, _objectCreationExpression);
                    if (existingArgument != null)
                    {
                        arguments.Add(existingArgument);
                        continue;
                    }

                    fieldDeclaration.SetName(fieldName);
                    _classDeclaration.AddClassMemberDeclaration((IClassMemberDeclaration) fieldDeclaration);

                    var initializeMockStatement = elementFactory.CreateStatement("$0 = $1.For<$2>();", fieldName, substituteClass, parameter.Type);
                    block.AddStatementBefore(initializeMockStatement, _objectCreationExpression.GetContainingStatement());

                    var argument = elementFactory.CreateArgument(ParameterKind.VALUE, elementFactory.CreateExpression("$0", fieldName));
                    arguments.Add(argument);
                }

            }

            arguments.Reverse();
            foreach (var oldArgument in _objectCreationExpression.ArgumentList.Arguments)
                _objectCreationExpression.RemoveArgument(oldArgument);

            foreach (var argument in arguments)
                _objectCreationExpression.AddArgumentAfter(argument, null);

            return _ => { };
        }

        private ICSharpArgument GetExistingArgument(IParameter parameter, string fieldName, IObjectCreationExpression objectCreationExpression)
        {
            foreach (var argument in objectCreationExpression.ArgumentList.Arguments)
            {
                if ((argument.Value as IReferenceExpression)?.NameIdentifier.Name == fieldName)
                    return argument;
            }

            if (!(parameter.Type is DeclaredTypeBase parameterType))
                return null;
            var parameterTypeName = parameterType.GetClrName().FullName;
            var matchingArguments = objectCreationExpression.ArgumentList.Arguments
                .Where(arg => ((arg.Value as IReferenceExpression)?.Reference.Resolve().DeclaredElement.Type() as DeclaredTypeBase)?.GetClrName().FullName == parameterTypeName).ToList();
            if (matchingArguments.Count != 1)
                return null;

            return matchingArguments.Single();
        }
    }
}