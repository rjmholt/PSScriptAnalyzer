using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class AssemblyRuleProvider : IRuleProvider
    {
        public static AssemblyRuleProvider FromAssembly(
            Assembly ruleAssembly,
            IScriptAnalyzerConfiguration configuration)
        {
            var ruleFactories = new Dictionary<RuleInfo, TypeRuleFactory<ScriptRule>>();

            foreach (Type exportedType in ruleAssembly.GetExportedTypes())
            {
                if (!RuleInfo.TryGetFromRuleType(exportedType, out RuleInfo ruleInfo))
                {
                    continue;
                }

                if (typeof(ScriptRule).IsAssignableFrom(exportedType))
                {
                    if (TryGetRuleFactory(ruleInfo, exportedType, configuration, out TypeRuleFactory<ScriptRule> factory))
                    {
                        ruleFactories[ruleInfo] = factory;
                    }

                    continue;
                }
            }

            return new AssemblyRuleProvider(ruleFactories);
        }

        private readonly IReadOnlyDictionary<RuleInfo, TypeRuleFactory<ScriptRule>> _scriptRuleFactories;

        internal AssemblyRuleProvider(
            IReadOnlyDictionary<RuleInfo, TypeRuleFactory<ScriptRule>> scriptRuleFactories)
        {
            _scriptRuleFactories = scriptRuleFactories;
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

        private static bool TryGetRuleFactory<TRuleBase>(
            RuleInfo ruleInfo,
            Type ruleType,
            IScriptAnalyzerConfiguration configuration,
            out TypeRuleFactory<TRuleBase> factory)
        {
            ConstructorInfo[] ruleConstructors = ruleType.GetConstructors();
            if (ruleConstructors.Length != 1)
            {
                factory = null;
                return false;
            }
            ConstructorInfo ruleConstructor = ruleConstructors[0];

            Type baseType = ruleType.BaseType;
            Type configurationType = null;
            while (baseType != null)
            {
                if (baseType.IsGenericType
                    && baseType.GetGenericTypeDefinition() == typeof(Rule<>))
                {
                    configurationType = baseType.GetGenericArguments()[0];
                    break;
                }

                baseType = baseType.BaseType;
            }

            IRuleConfiguration ruleConfiguration = null;
            if (configurationType == null)
            {
            }

            if (ruleInfo.IsIdempotent)
            {
                factory = new ConstructorInjectionIdempotentRuleFactory<TRuleBase>(
                    ruleInfo,
                    ruleConstructor,
                    ruleConfiguration);
                return true;
            }

            if (typeof(IResettable).IsAssignableFrom(ruleType))
            {
                factory = new ConstructorInjectingResettableRulePoolingFactory<TRuleBase>(
                    ruleInfo,
                    ruleConstructor,
                    ruleConfiguration);
                return true;
            }

            if (typeof(IDisposable).IsAssignableFrom(ruleType))
            {
                factory = new ConstructorInjectingDisposableRuleFactory<TRuleBase>(
                    ruleInfo,
                    ruleConstructor,
                    ruleConfiguration);
                return true;
            }

            factory = new ConstructorInjectingRuleFactory<TRuleBase>(
                ruleInfo,
                ruleConstructor,
                ruleConfiguration);
            return true;
        }
    }
}
