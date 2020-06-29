using Microsoft.PowerShell.ScriptAnalyzer.Builtin;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System;
using System.Collections.Generic;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public class ScriptAnalyzerBuilder
    {
        private readonly List<IRuleProviderFactory> _ruleProviderFactories;

        private IRuleExecutorFactory _ruleExecutorFactory;

        private RuleComponentProvider _ruleComponentProvider;

        private Dictionary<string, IRuleConfiguration> _ruleConfigurations;

        public ScriptAnalyzerBuilder()
        {
            _ruleProviderFactories = new List<IRuleProviderFactory>();
        }

        public ScriptAnalyzerBuilder WithRuleConfiguration(string rule, IRuleConfiguration configuration)
        {
            if (_ruleConfigurations == null)
            {
                _ruleConfigurations = new Dictionary<string, IRuleConfiguration>();
            }

            _ruleConfigurations[rule] = configuration;

            return this;
        }

        public ScriptAnalyzerBuilder WithRuleConfiguration(IReadOnlyDictionary<string, IRuleConfiguration> configurations)
        {
            if (_ruleConfigurations == null)
            {
                _ruleConfigurations = new Dictionary<string, IRuleConfiguration>();
            }

            foreach (KeyValuePair<string, IRuleConfiguration> configuration in configurations)
            {
                _ruleConfigurations[configuration.Key] = configuration.Value;
            }

            return this;
        }

        public ScriptAnalyzerBuilder WithRuleExecutorFactory(IRuleExecutorFactory ruleExecutorFactory)
        {
            _ruleExecutorFactory = ruleExecutorFactory;
            return this;
        }

        public ScriptAnalyzerBuilder WithRuleComponentProvider(RuleComponentProvider ruleComponentProvider)
        {
            _ruleComponentProvider = ruleComponentProvider;
            return this;
        }

        public ScriptAnalyzerBuilder WithRuleComponentProvider(Action<RuleComponentProviderBuilder> configureComponentProviderBuilder)
        {
            var componentProviderBuilder = new RuleComponentProviderBuilder();
            configureComponentProviderBuilder(componentProviderBuilder);
            WithRuleComponentProvider(componentProviderBuilder.Build());
            return this;
        }

        public ScriptAnalyzerBuilder AddRuleProviderFactory(IRuleProviderFactory ruleProvider)
        {
            _ruleProviderFactories.Add(ruleProvider);
            return this;
        }

        public ScriptAnalyzerBuilder AddBuiltinRules()
        {
            _ruleProviderFactories.Add(
                new BuiltinRuleProviderFactory());
            return this;
        }

        public ScriptAnalyzerBuilder AddBuiltinRules(Action<BuiltinRulesBuilder> configureBuiltinRules)
        {
            var builtinRulesBuilder = new BuiltinRulesBuilder();
            configureBuiltinRules(builtinRulesBuilder);
            _ruleProviderFactories.Add(builtinRulesBuilder.Build());
            return this;
        }

        public ScriptAnalyzer Build()
        {
            return ScriptAnalyzer.Create(
                _ruleComponentProvider ?? Default.RuleComponentProvider,
                _ruleExecutorFactory ?? Default.RuleExecutorFactory,
                _ruleProviderFactories,
                _ruleConfigurations ?? Default.RuleConfiguration);
        }
    }
}
