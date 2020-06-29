using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Runtime;
using Microsoft.PowerShell.ScriptAnalyzer.Utils;
using System.IO;
using System.Management.Automation;
using SMA = System.Management.Automation;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public static class ConfiguredBuilding
    {
        public static ScriptAnalyzer CreateScriptAnalyzer(this IScriptAnalyzerConfiguration configuration)
        {
            var analyzerBuilder = new ScriptAnalyzerBuilder();

            switch (configuration.BuiltinRules ?? BuiltinRulePreference.Default)
            {
                case BuiltinRulePreference.Aggressive:
                case BuiltinRulePreference.Default:
                    analyzerBuilder.AddBuiltinRules();
                    break;
            }

            switch (configuration.RuleExecution ?? RuleExecutionMode.Default)
            {
                case RuleExecutionMode.Default:
                case RuleExecutionMode.Parallel:
                    analyzerBuilder.WithRuleExecutorFactory(new ParallelLinqRuleExecutorFactory());
                    break;

                case RuleExecutionMode.Sequential:
                    analyzerBuilder.WithRuleExecutorFactory(new SequentialRuleExecutorFactory());
                    break;
            }

            if (configuration.RulePaths != null)
            {
                foreach (string rulePath in configuration.RulePaths)
                {
                    string extension = Path.GetExtension(rulePath);

                    // TODO: Deal with relative paths

                    if (extension.CaseInsensitiveEquals(".dll"))
                    {
                        analyzerBuilder.AddRuleProviderFactory(TypeRuleProviderFactory.FromAssemblyFile(rulePath));
                        continue;
                    }

                    analyzerBuilder.AddRuleProviderFactory(new PSModuleRuleProviderFactory(rulePath));
                }
            }

            return analyzerBuilder.Build();
        }
    }
}
