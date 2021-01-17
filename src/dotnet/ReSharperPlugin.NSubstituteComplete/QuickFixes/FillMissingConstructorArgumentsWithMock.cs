using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.ExpectedTypes;
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
using ReSharperPlugin.NSubstituteComplete.Helpers;
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
            var mockAliases = NSubstituteCompleteSettingsHelper.GetSettings(solution)
                .GetMockAliases();

            var lastInitializedSubstitute = block
                .Children()
                .OfType<IExpressionStatement>()
                .Where(statement => statement.GetTreeStartOffset().Offset < _objectCreationExpression.GetContainingStatement().GetTreeStartOffset().Offset)
                .LastOrDefault(statement => IsMockInitializer(statement, mockAliases));

            var arguments = new LocalList<ICSharpArgument>();
            for (var argumentIndex = 0; argumentIndex < targetConstructor.Parameters.Count; argumentIndex++)
            {
                var parameter = targetConstructor.Parameters[argumentIndex];
                if (!(parameter.Type is DeclaredTypeBase declaredTypeBase))
                    continue;

                if (declaredTypeBase.GetClrName().FullName == "System.String")
                {
                    var argument = elementFactory.CreateArgument(ParameterKind.VALUE, elementFactory.CreateExpression("$0", $@"""some-{TextHelper.ToKebabCase(parameter.ShortName)}"""));
                    arguments.Add(argument);
                }
                else
                {
                    var expectedTypeConstraint = new CSharpImplicitlyConvertibleToConstraint(parameter.Type, cSharpTypeConversionRule, cSharpTypeConstraintsVerifier);
                    var options = new SuggestionOptions(defaultName: declaredTypeBase.GetClrName().ShortName);
                    var (mockedType, useNSubstituteMock) = GetMockedType(declaredTypeBase, mockAliases, expectedTypeConstraint);
                    var fieldDeclaration = elementFactory.CreateTypeMemberDeclaration("private $0 $1;", mockedType, declaredTypeBase.GetClrName().ShortName);

                    var fieldName = psiServices.Naming.Suggestion.GetDerivedName(fieldDeclaration.DeclaredElement, NamedElementKinds.PrivateInstanceFields, ScopeKind.Common, _classDeclaration.Language, options, psiSourceFile);

                    var existingArgument = GetExistingArgument(expectedTypeConstraint, fieldName, _objectCreationExpression, argumentIndex);
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

                    var argument = CreateValidArgument(elementFactory, expectedTypeConstraint, mockedType, fieldName, treeNode.GetPsiModule());
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

        private static bool IsMockInitializer(IExpressionStatement statement, Dictionary<string, List<(string TargetTypeExpression, string ClrMockedType)>> mockAliases)
        {
            var invocationExpression = statement.Expression.Children().OfType<IInvocationExpression>().FirstOrDefault();
            if (invocationExpression != null)
                if (invocationExpression.GetText().StartsWith("Substitute.For"))
                    return true;

            var objectCreationExpression = statement.Expression.Children().OfType<IObjectCreationExpression>().FirstOrDefault();
            if (objectCreationExpression?.Type() is DeclaredTypeBase declaredTypeBase)
                if (mockAliases.Values.SelectMany(a => a).Any(a => a.ClrMockedType == declaredTypeBase.GetClrName().FullName))
                    return true;

            return false;
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

        private static (IType mockedType, bool useNSubstituteMock) GetMockedType(
            DeclaredTypeBase parameterType,
            Dictionary<string, List<(string TargetTypeExpression, string ClrMockedType)>> mockAliases,
            CSharpImplicitlyConvertibleToConstraint expectedType
        )
        {
            var parameterTypeFullName = parameterType.GetClrName().FullName;

            if (mockAliases.TryGetValue(parameterTypeFullName, out var aliasTypes))
            {
                if (aliasTypes.Count == 1)
                {
                    var aliasType = aliasTypes.Single();
                    var type = TypeFactory.CreateTypeByCLRName(aliasType.ClrMockedType, parameterType.Module);
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

                foreach (var (targetTypeExpression, clrMockedType) in aliasTypes)
                {
                    var targetType = TypeHelper.ParseGenericType(targetTypeExpression, parameterType.Module);
                    if (expectedType.Accepts(targetType))
                        return (TypeFactory.CreateTypeByCLRName(clrMockedType, parameterType.Module), false);
                }

                foreach (var aliasType in aliasTypes)
                {
                    var type = TypeFactory.CreateTypeByCLRName(aliasType.ClrMockedType, parameterType.Module);
                    if (expectedType.Accepts(type))
                        return (type, false);
                }
            }

            return (parameterType, true);
        }

        private ICSharpArgument GetExistingArgument(IExpectedTypeConstraint expectedTypeConstraint, string fieldName, IObjectCreationExpression objectCreationExpression, int argumentIndex)
        {
            if (argumentIndex < objectCreationExpression.ArgumentList.Arguments.Count)
            {
                var currentArgument = objectCreationExpression.ArgumentList.Arguments.Skip(argumentIndex).First();
                if (expectedTypeConstraint.Accepts(currentArgument.Value.Type()))
                {
                    return currentArgument;
                }
            }

            foreach (var argument in objectCreationExpression.ArgumentList.Arguments.Where(argument => (argument.Value as IReferenceExpression)?.NameIdentifier.Name == fieldName))
            {
                return argument;
            }

            var matchingArguments = objectCreationExpression.ArgumentList.Arguments
                .Select((arg, i) => (argument: arg, argumentIndex: i))
                .Where(a => expectedTypeConstraint.Accepts(a.argument.Value.Type()))
                .ToList();
            if (matchingArguments.Count == 0)
                return null;
            if (matchingArguments.Count != 1)
                return matchingArguments.Single().argument;

            return matchingArguments
                .OrderBy(x => Math.Abs(x.argumentIndex - argumentIndex))
                .First().argument;
        }
    }
}
