using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd
{
    public class PsdScriptAnalyzerConfiguration : IScriptAnalyzerConfiguration
    {
        private readonly IReadOnlyDictionary<string, HashtableAst> _ruleConfigurations;

        private readonly ConcurrentDictionary<string, IRuleConfiguration> _ruleConfigurationCache;

        private readonly PsdTypedObjectConverter _psdConverter;

        public PsdScriptAnalyzerConfiguration(
            PsdTypedObjectConverter psdConverter,
            IReadOnlyList<string> rulePaths,
            IReadOnlyDictionary<string, HashtableAst> ruleConfigurations)
        {
            _ruleConfigurations = ruleConfigurations;
            _ruleConfigurationCache = new ConcurrentDictionary<string, IRuleConfiguration>();
            _psdConverter = psdConverter;
            RulePaths = rulePaths;
        }

        public IReadOnlyList<string> RulePaths { get; }

        public bool TryGetRuleConfiguration(Type configurationType, string ruleName, out IRuleConfiguration configuration)
        {
            try
            {
                configuration = _ruleConfigurationCache.GetOrAdd(ruleName, (ruleName) => GetConfigurationForRule(ruleName, configurationType));
                return true;
            }
            catch (ConfigurationNotFoundException)
            {
                configuration = default;
                return false;
            }
        }

        private IRuleConfiguration GetConfigurationForRule(string ruleName, Type ruleConfigurationType)
        {
            if (!_ruleConfigurations.TryGetValue(ruleName, out HashtableAst configurationAst))
            {
                throw new ConfigurationNotFoundException();
            }

            if (ruleConfigurationType == typeof(IRuleConfiguration))
            {
                return (IRuleConfiguration)_psdConverter.Convert(typeof(RuleConfiguration), configurationAst);
            }

            return (IRuleConfiguration)_psdConverter.Convert(ruleConfigurationType, configurationAst);
        }
    }
}
