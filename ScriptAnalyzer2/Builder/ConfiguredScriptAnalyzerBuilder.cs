using Microsoft.PowerShell.ScriptAnalyzer.Builtin;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public class ConfigurationScriptAnalyzerFactory : ScriptAnalyzerFactory
    {
        private IScriptAnalyzerConfiguration _configuration;

        public ConfigurationScriptAnalyzerFactory(IScriptAnalyzerConfiguration scriptAnalyzerConfiguration)
        {
            _configuration = scriptAnalyzerConfiguration;
        }

        protected override IRuleExecutorFactory GetExecutorFactory()
        {
            switch (_configuration.RuleExecution)
            {
                case RuleExecutionMode.Parallel:
                    return new ParallelLinqRuleExecutorFactory();

                case RuleExecutionMode.Sequential:
                    return new SequentialRuleExecutorFactory();

                default:
                    return new ParallelLinqRuleExecutorFactory();
            }
        }

        protected override IRuleProvider GetRuleProvider()
        {
            if (_configuration.RulePaths == null || _configuration.RulePaths.Count == 0)
            {
                return BuiltinRuleProvider.WithConfiguration(_configuration);
            }

            var ruleProviders = new List<IRuleProvider>() { BuiltinRuleProvider.WithConfiguration(_configuration) };

            foreach (string rulePath in _configuration.RulePaths)
            {
                if (string.Equals(Path.GetExtension(rulePath), ".dll", StringComparison.OrdinalIgnoreCase))
                {
                    ruleProviders.Add(TypeRuleProvider.FromAssemblyFile(_configuration, rulePath));
                }
            }

            return new CompositeRuleProvider(ruleProviders);
        }
    }
}
