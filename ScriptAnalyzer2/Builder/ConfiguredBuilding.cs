using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Runtime;
using Microsoft.PowerShell.ScriptAnalyzer.Utils;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using SMA = System.Management.Automation;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public static class ConfiguredBuilding
    {
        private static readonly IReadOnlyList<string> s_moduleExtensions = new[]
        {
            ".psd1",
            ".psm1"
        };

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
                foreach (string configuredRulePath in configuration.RulePaths)
                {
                    string rulePath = Path.IsPathRooted(configuredRulePath)
                        ? configuredRulePath
                        : Path.GetFullPath(
                            Path.Combine(configuration.BasePath, configuredRulePath));

                    string extension = Path.GetExtension(rulePath);

                    // DLL with rule implementations in it
                    if (extension.CaseInsensitiveEquals(".dll"))
                    {
                        analyzerBuilder.AddRuleProviderFactory(TypeRuleProviderFactory.FromAssemblyFile(rulePath));
                        continue;
                    }

                    // Module within a directory
                    if (string.IsNullOrEmpty(extension))
                    {
                        string dirName = Path.GetFileName(rulePath);

                        foreach (string moduleExtension in s_moduleExtensions)
                        {
                            if (File.Exists(Path.Combine(rulePath, $"{dirName}{moduleExtension}")))
                            {
                                analyzerBuilder.AddRuleProviderFactory(new PSModuleRuleProviderFactory(rulePath));
                            }
                        }

                        // TODO: Error about not being valid

                        continue;
                    }

                    // Bare module
                    if (extension.CaseInsensitiveEquals(".psd1") || extension.CaseInsensitiveEquals(".psm1"))
                    {
                        analyzerBuilder.AddRuleProviderFactory(new PSModuleRuleProviderFactory(rulePath));
                        continue;
                    }
                }
            }

            return analyzerBuilder.Build();
        }
    }
}
