using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;

namespace ReSharperPlugin.NSubstituteComplete.Options;

public static class NSubstituteCompleteSettingsHelper
{
    public static NSubstituteCompleteSettings GetSettings(ISolution solution)
    {
        var settingsStore = solution.GetComponent<ISettingsStore>();
        var settingsOptimization = solution.GetComponent<ISettingsOptimization>();
        var contextBoundSettingsStore =
            settingsStore.BindToContextTransient(ContextRange.Smart(solution.ToDataContext()));
        return contextBoundSettingsStore.GetKey<NSubstituteCompleteSettings>(settingsOptimization);
    }

    public static Dictionary<string, List<(string TargetTypeExpression, string ClrMockedType)>> GetMockAliases(this NSubstituteCompleteSettings settings)
    {
        var rawAliases = settings.MockAliases
            .Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split(['='], 2))
            .Where(x => x.Length == 2);

        var aliases = new Dictionary<string, List<(string TargetTypeExpression, string ClrMockedType)>>();
        foreach (var alias in rawAliases)
        {
            var targetType = alias[0];
            var mockedType = alias[1];

            var targetTypeClr = GetAsClrTypeName(targetType);
            if (!aliases.TryGetValue(targetTypeClr, out var aliasDefinitions))
            {
                aliasDefinitions = new List<(string TargetTypeExpression, string ClrMockedType)>();
                aliases.Add(targetTypeClr, aliasDefinitions);
            }

            aliasDefinitions.Add((targetType, GetAsClrTypeName(mockedType)));
        }

        return aliases;
    }

    private static string GetAsClrTypeName(string type)
    {
        var genericMarkerIndex = type.IndexOf('<');
        if (genericMarkerIndex == -1)
            return type;
        var endType = type.Substring(genericMarkerIndex);
        var parameterCount = endType.Count(x => x == ',') + 1;
        return type.Remove(genericMarkerIndex) + "`" + parameterCount;
    }
}
