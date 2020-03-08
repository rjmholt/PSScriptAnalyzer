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

        public TRuleConfiguration GetRuleConfiguration<TRuleConfiguration>(string ruleName) where TRuleConfiguration : IRuleConfiguration
        {
            return (TRuleConfiguration)_ruleConfigurationCache.GetOrAdd(ruleName, (ruleName) => _psdConverter.Convert<TRuleConfiguration>(_ruleConfigurations[ruleName]));
        }
    }
}
