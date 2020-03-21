using Microsoft.PowerShell.ScriptAnalyzer.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd
{
    public class PsdScriptAnalyzerConfiguration : IScriptAnalyzerConfiguration
    {
        public static PsdScriptAnalyzerConfiguration FromFile(string filePath)
        {
            return FromAst(PowerShellParsing.ParseHashtableFromFile(filePath));
        }

        public static PsdScriptAnalyzerConfiguration FromString(string hashtableString)
        {
            return FromAst(PowerShellParsing.ParseHashtableFromInput(hashtableString));
        }

        public static PsdScriptAnalyzerConfiguration FromAst(HashtableAst ast)
        {
            var psdConverter = new PsdTypedObjectConverter();

            var configuration = psdConverter.Convert<IReadOnlyDictionary<string, ExpressionAst>>(ast);
            var ruleExecutionMode = psdConverter.Convert<RuleExecutionMode>(configuration["RuleExecution"]);
            var rulePaths = psdConverter.Convert<IReadOnlyList<string>>(configuration["RulePaths"]);
            var ruleConfigurations = psdConverter.Convert<IReadOnlyDictionary<string, HashtableAst>>(configuration["Rules"]);

            return new PsdScriptAnalyzerConfiguration(psdConverter, ruleExecutionMode, rulePaths, ruleConfigurations);
        }

        private readonly IReadOnlyDictionary<string, HashtableAst> _ruleConfigurations;

        private readonly ConcurrentDictionary<string, IRuleConfiguration> _ruleConfigurationCache;

        private readonly PsdTypedObjectConverter _psdConverter;

        public PsdScriptAnalyzerConfiguration(
            PsdTypedObjectConverter psdConverter,
            RuleExecutionMode ruleExecutionMode,
            IReadOnlyList<string> rulePaths,
            IReadOnlyDictionary<string, HashtableAst> ruleConfigurations)
        {
            _ruleConfigurations = ruleConfigurations;
            _ruleConfigurationCache = new ConcurrentDictionary<string, IRuleConfiguration>();
            _psdConverter = psdConverter;
            RuleExecution = ruleExecutionMode;
            RulePaths = rulePaths;
        }

        public RuleExecutionMode RuleExecution { get; }

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
