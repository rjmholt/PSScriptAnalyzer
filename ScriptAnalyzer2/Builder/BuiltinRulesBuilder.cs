using Microsoft.PowerShell.ScriptAnalyzer.Builtin;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public class BuiltinRulesBuilder
    {
        private IRuleConfigurationCollection _ruleConfiguration;

        private IRuleComponentProvider _ruleComponents;

        public BuiltinRulesBuilder WithRuleConfiguration(IRuleConfigurationCollection ruleConfigurationCollection)
        {
            _ruleConfiguration = ruleConfigurationCollection;
            return this;
        }

        public BuiltinRulesBuilder WithRuleComponentProvider(IRuleComponentProvider ruleComponentProvider)
        {
            _ruleComponents = ruleComponentProvider;
            return this;
        }

        public BuiltinRuleProvider Build()
        {
            return BuiltinRuleProvider.Create(
                _ruleConfiguration ?? Default.RuleConfiguration,
                _ruleComponents ?? Default.RuleComponentProvider);
        }
    }
}
