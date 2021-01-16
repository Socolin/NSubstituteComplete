using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util.Extension;

namespace ReSharperPlugin.NSubstituteComplete
{
    public class TypeHelper
    {
        public static IType ParseGenericType(string typeExpressionText, IPsiModule module)
        {
            var genericMarkerIndex = typeExpressionText.IndexOf('<');
            if (genericMarkerIndex == -1)
                return TypeFactory.CreateTypeByCLRName(typeExpressionText, module);

            var endType = typeExpressionText.Substring(genericMarkerIndex);
            var parameterTypesExpression = endType.RemoveStart("<").RemoveEnd(">");
            var parameterTypesText = parameterTypesExpression.Split(",");

            var clrTypeName = typeExpressionText.Remove(genericMarkerIndex) + "`" + parameterTypesText.Length;
            var type = TypeFactory.CreateTypeByCLRName(clrTypeName, module);

            var dictionary = new Dictionary<ITypeParameter, IType>();
            var typeElement = type.GetTypeElement();
            var allTypeParameters = typeElement.GetAllTypeParameters();
            for (var index = 0; index < allTypeParameters.Count; index++)
            {
                var typeParameter = allTypeParameters[index];
                dictionary[typeParameter] = ParseGenericType(parameterTypesText[index], module);
            }

            return TypeFactory.CreateType(typeElement, EmptySubstitution.INSTANCE.Extend(dictionary), type.NullableAnnotation);
        }
    }
}
