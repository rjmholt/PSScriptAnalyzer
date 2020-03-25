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

        public BuiltinRulePreference? BuiltinRules { get; }

        public RuleExecutionMode? RuleExecution { get; }

        public IReadOnlyList<string> RulePaths { get; }

        public IReadOnlyDictionary<string, IRuleConfiguration> RuleConfiguration { get; }
    }

    public class PsdRuleConfiguration : LazyConvertedRuleConfiguration<HashtableAst>
    {
        private readonly PsdTypedObjectConverter _psdConverter;

        public PsdRuleConfiguration(
            PsdTypedObjectConverter psdConverter,
            CommonConfiguration common,
            HashtableAst configurationHashtableAst)
            : base(common, configurationHashtableAst)
        {
            _psdConverter = psdConverter;
        }

        public override bool TryConvertObject(Type type, HashtableAst configuration, out object result)
        {
            try
            {
                result = _psdConverter.Convert(type, configuration);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}
