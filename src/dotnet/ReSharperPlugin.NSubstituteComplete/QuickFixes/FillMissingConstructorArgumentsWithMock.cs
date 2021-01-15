using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.I18n.Services;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.ExpectedTypes;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Naming.Extentions;
using JetBrains.ReSharper.Psi.Naming.Impl;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.Util.PaternMatching;
using ReSharperPlugin.NSubstituteComplete.Options;

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

            if (_objectCreationExpression.GetProject()?.GetAllReferencedAssemblies().Any(x => x.Name == "NSubstitute") != true)
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
            var cSharpTypeConversionRule = treeNode.GetTypeConversionRule();
            var cSharpTypeConstraintsVerifier = new CSharpTypeConstraintsVerifier(treeNode.GetCSharpLanguageLevel(), cSharpTypeConversionRule);

            var lastInitializedSubstitute = block
                .Children()
                .OfType<IExpressionStatement>()
                .Where(statement => statement.GetTreeStartOffset().Offset < _objectCreationExpression.GetContainingStatement().GetTreeStartOffset().Offset)
                .Where(statement => statement.Expression.Children().OfType<IInvocationExpression>().FirstOrDefault()?.GetText().StartsWith("Substitute.For") == true)
                .LastOrDefault();

            var mockAliases = NSubstituteCompleteSettingsHelper.GetSettings(solution)
                .GetMockAliases();

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
                    var (mockedType, useNSubstituteMock) = GetMockedType(declaredTypeBase, mockAliases);
                    var options = new SuggestionOptions(defaultName: declaredTypeBase.GetClrName().ShortName);
                    var fieldDeclaration = elementFactory.CreateTypeMemberDeclaration("private $0 $1;", mockedType, declaredTypeBase.GetClrName().ShortName);

                    var fieldName = psiServices.Naming.Suggestion.GetDerivedName(fieldDeclaration.DeclaredElement, NamedElementKinds.PrivateInstanceFields, ScopeKind.Common, _classDeclaration.Language, options, psiSourceFile);

                    var existingArgument = GetExistingArgument(parameter, fieldName, _objectCreationExpression);
                    if (existingArgument != null)
                    {
                        arguments.Add(existingArgument);
                        continue;
                    }

                    fieldDeclaration.SetName(fieldName);
                    _classDeclaration.AddClassMemberDeclaration((IClassMemberDeclaration) fieldDeclaration);

                    ICSharpStatement initializeMockStatement;
                    if (useNSubstituteMock)
                        initializeMockStatement = elementFactory.CreateStatement("$0 = $1.For<$2>();", fieldName, substituteClass, mockedType);
                    else
                        initializeMockStatement = elementFactory.CreateStatement("$0 = new $1();", fieldName, mockedType);

                    if (lastInitializedSubstitute == null)
                        block.AddStatementBefore(initializeMockStatement, _objectCreationExpression.GetContainingStatement());
                    else
                        block.AddStatementAfter(initializeMockStatement, lastInitializedSubstitute);

                    var argument = CreateValidArgument(elementFactory, new CSharpImplicitlyConvertibleToConstraint(parameter.Type, cSharpTypeConversionRule, cSharpTypeConstraintsVerifier), mockedType, fieldName, treeNode.GetPsiModule());
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

        private ICSharpArgument CreateValidArgument(CSharpElementFactory elementFactory, IExpectedTypeConstraint expectedTypeConstraint, IType mockedType, string fieldName, IPsiModule module)
        {
            if (!expectedTypeConstraint.Accepts(mockedType))
            {
                var typeElement = mockedType.GetTypeElement();
                if (typeElement != null)
                {
                    foreach (var member in typeElement.GetMembers())
                    {
                        var type = member.Type();
                        if (type == null)
                            continue;

                        var accessRights = member.GetAccessRights();
                        if (accessRights == AccessRights.INTERNAL && member is IClrDeclaredElement clrDeclaredElement && clrDeclaredElement.Module.AreInternalsVisibleTo(module))
                            accessRights = AccessRights.PUBLIC;
                        if (accessRights != AccessRights.PUBLIC)
                            continue;

                        if (expectedTypeConstraint.Accepts(type))
                            return elementFactory.CreateArgument(ParameterKind.VALUE, elementFactory.CreateExpression("$0.$1", fieldName, member.ShortName));
                    }
                }
            }

            return elementFactory.CreateArgument(ParameterKind.VALUE, elementFactory.CreateExpression("$0", fieldName));
        }

        private static (IType mockedType, bool useNSubstituteMock) GetMockedType(DeclaredTypeBase parameterType, Dictionary<string, string> mockAliases)
        {
            var parameterTypeFullName = parameterType.GetClrName().FullName;

            if (mockAliases.TryGetValue(parameterTypeFullName, out var aliasType))
            {
                var type = TypeFactory.CreateTypeByCLRName(aliasType, parameterType.Module);
                var typeElement = type.GetTypeElement();
                if (typeElement != null
                    && parameterType.GetClrName().TypeParametersCount == type.GetClrName().TypeParametersCount
                    && parameterType.GetClrName().TypeParametersCount > 0)
                {
                    var dictionary = new Dictionary<ITypeParameter, IType>();
                    var fromDictionary = parameterType.GetSubstitution().ToDictionary();
                    foreach (var typeParameter in typeElement.GetAllTypeParameters())
                    {
                        dictionary[typeParameter] = fromDictionary.Single(x => x.Key.Index == typeParameter.Index).Value;
                    }

                    return (TypeFactory.CreateType(typeElement, EmptySubstitution.INSTANCE.Extend(dictionary), type.NullableAnnotation), false);
                }

                if (typeElement != null
                    && parameterType.GetClrName().TypeParametersCount == 0 && type.GetClrName().TypeParametersCount == 1)
                {
                    var dictionary = new Dictionary<ITypeParameter, IType>();
                    foreach (var typeParameter in typeElement.GetAllTypeParameters())
                    {
                        dictionary[typeParameter] = parameterType;
                    }

                    return (TypeFactory.CreateType(typeElement, EmptySubstitution.INSTANCE.Extend(dictionary), type.NullableAnnotation), false);
                }

                return (type, false);
            }

            /*
            var typeElement = parameterType.GetTypeElement();
            if (parameterType.GetClrName().TypeParametersCount > 0 && typeElement != null)
            {
                var parameterTypeString = "<" + string.Join(",", typeElement.TypeParameters.Select(t => t.ShortName)) + ">";
                var buildType = "<" + string.Join(",", typeElement.TypeParameters.Select(t => t.ShortName)) + ">";
                var parameterNameWithGenericTypes = parameterTypeFullName.Replace("`" + parameterType.GetClrName().TypeParametersCount, parameterTypeString);

                if (parameterTypeFullName.Contains('`'))
                    if (mockAliases.TryGetValue(parameterNameWithGenericTypes, out var aliasTypeFromGeneric))
                    {
                        var aliasGenericIndex = aliasTypeFromGeneric.IndexOf('<');
                        if (aliasGenericIndex != -1)
                        {
                            var typeByClrName = TypeFactory.CreateTypeByCLRName(aliasTypeFromGeneric, parameterType.Module);
                            aliasTypeFromGeneric = aliasTypeFromGeneric.Remove(aliasGenericIndex) + "`" + ;
                                                parameterType.GetSubstitution().Apply(TypeFactory.CreateTypeByCLRName("TestProject1.FakeDep6`2", parameterType.Module))

                            aliasTypeFromGeneric.Remove(aliasGenericIndex) + "`" + ;
                            return (typeByClrName, false);

                        }
                        return (TypeFactory.CreateTypeByCLRName(aliasTypeFromGeneric, parameterType.Module), false);
                    }
            }
            */

            return (parameterType, true);
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
