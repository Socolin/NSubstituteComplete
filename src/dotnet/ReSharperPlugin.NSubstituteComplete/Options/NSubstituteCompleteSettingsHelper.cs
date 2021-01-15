using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;

namespace ReSharperPlugin.NSubstituteComplete.Options
{
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

        public static Dictionary<string, string> GetMockAliases(this NSubstituteCompleteSettings settings)
        {
            return settings.MockAliases
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new[] {'='}, 2))
                .Where(x => x.Length == 2)
                .Select(x => new[] {ReplaceGenericTypeWithRiderNotation(x[0]), ReplaceGenericTypeWithRiderNotation(x[1])})
                .ToDictionary(x => x[0], x => x[1]);
        }

        private static string ReplaceGenericTypeWithRiderNotation(string type)
        {
            var genericMarkerIndex = type.IndexOf('<');
            if (genericMarkerIndex == -1)
                return type;
            var endType = type.Substring(genericMarkerIndex);
            var parameterCount = endType.Count(x => x == ',') + 1;
            return type.Remove(genericMarkerIndex) + "`" + parameterCount;
        }
    }
}
