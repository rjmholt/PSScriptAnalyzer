using Microsoft.PowerShell.ScriptAnalyzer.Builder;
using Microsoft.PowerShell.ScriptAnalyzer.Builtin;
using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration
{
    public static class ScriptAnalyzerConfigurationExtensions
    {
        public static IRuleProvider GetRuleProvider(
            this IScriptAnalyzerConfiguration configuration)
        {
            return configuration.GetRuleProvider(configuration.GetRuleComponentProvider());
        }

        public static IRuleProvider GetRuleProvider(
            this IScriptAnalyzerConfiguration configuration,
            IRuleComponentProvider ruleComponentProvider)
        {
            var builtinRuleProvider = BuiltinRuleProvider.Create(configuration.RuleConfiguration, ruleComponentProvider);

            if (configuration.RulePaths == null || configuration.RulePaths.Count == 0)
            {
                return builtinRuleProvider;
            }

            var ruleProviders = new List<IRuleProvider>() { builtinRuleProvider };

            foreach (string rulePath in configuration.RulePaths)
            {
                if (string.Equals(Path.GetExtension(rulePath), ".dll", StringComparison.OrdinalIgnoreCase))
                {
                    ruleProviders.Add(TypeRuleProvider.FromAssemblyFile(configuration.RuleConfiguration, ruleComponentProvider, rulePath));
                }
            }

            return new CompositeRuleProvider(ruleProviders);
        }

        public static IRuleExecutorFactory GetRuleExecutorFactory(
            this IScriptAnalyzerConfiguration configuration)
        {
            switch (configuration.RuleExecution)
            {
                case RuleExecutionMode.Parallel:
                    return new ParallelLinqRuleExecutorFactory();

                case RuleExecutionMode.Sequential:
                    return new SequentialRuleExecutorFactory();

                default:
                    return new ParallelLinqRuleExecutorFactory();
            }
        }
    }
}
