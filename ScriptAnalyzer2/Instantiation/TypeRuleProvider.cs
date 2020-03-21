using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class TypeRuleProvider : IRuleProvider
    {
        public static TypeRuleProvider FromAssemblyFile(IScriptAnalyzerConfiguration configuration, string assemblyPath)
        {
            return FromAssembly(configuration, Assembly.LoadFile(assemblyPath));
        }

        public static TypeRuleProvider FromAssembly(
            IScriptAnalyzerConfiguration configuration,
            Assembly ruleAssembly)
        {
            return FromTypes(configuration, ruleAssembly.GetExportedTypes());
        }

        public static TypeRuleProvider FromTypes(IScriptAnalyzerConfiguration configuration, IReadOnlyList<Type> types)
        {
            return new TypeRuleProvider(GetRuleFactoriesFromTypes(configuration, types));
        }

        internal static IReadOnlyDictionary<RuleInfo, TypeRuleFactory<ScriptRule>> GetRuleFactoriesFromTypes(
            IScriptAnalyzerConfiguration configuration,
            IReadOnlyList<Type> types)
        {
            var ruleFactories = new Dictionary<RuleInfo, TypeRuleFactory<ScriptRule>>();

            foreach (Type type in types)
            {
                if (RuleGeneration.TryGetRuleFromType(configuration, type, out RuleInfo ruleInfo, out TypeRuleFactory<ScriptRule> factory))
                {
                    ruleFactories[ruleInfo] = factory;
                }
            }

            return ruleFactories;
        }

        private readonly IReadOnlyDictionary<RuleInfo, TypeRuleFactory<ScriptRule>> _scriptRuleFactories;

        internal TypeRuleProvider(
            IReadOnlyDictionary<RuleInfo, TypeRuleFactory<ScriptRule>> scriptRuleFactories)
        {
            _scriptRuleFactories = scriptRuleFactories;
        }

        public IEnumerable<RuleInfo> GetRuleInfos()
        {
            return _scriptRuleFactories.Keys;
        }

        public IEnumerable<ScriptRule> GetScriptRules()
        {
            foreach (TypeRuleFactory<ScriptRule> ruleFactory in _scriptRuleFactories.Values)
            {
                yield return ruleFactory.GetRuleInstance();
            }
        }

        public void ReturnRule(Rule rule)
        {
            if (!(rule is ScriptRule scriptRule))
            {
                return;
            }

            if (_scriptRuleFactories.TryGetValue(rule.RuleInfo, out TypeRuleFactory<ScriptRule> astRuleFactory))
            {
                astRuleFactory.ReturnRuleInstance(scriptRule);
            }
        }

    }
}
