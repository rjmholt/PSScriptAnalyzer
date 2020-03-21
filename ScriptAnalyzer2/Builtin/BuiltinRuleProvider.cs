using Microsoft.PowerShell.ScriptAnalyzer.Builtin.Rules;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.BuiltinRules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builtin
{
    public class BuiltinRuleProvider : TypeRuleProvider
    {
        private static readonly IReadOnlyList<Type> s_defaultRules = new []
        {
            typeof(AvoidEmptyCatchBlock),
            typeof(AvoidGlobalVars),
            typeof(AvoidPositionalParameters),
            typeof(AvoidUsingWMICmdlet),
            typeof(UseDeclaredVarsMoreThanAssignments),
            typeof(UseShouldProcessForStateChangingFunctions),
        };

        public static BuiltinRuleProvider WithConfiguration(IScriptAnalyzerConfiguration configuration)
        {
            return new BuiltinRuleProvider(TypeRuleProvider.GetRuleFactoriesFromTypes(configuration, s_defaultRules));
        }

        private BuiltinRuleProvider(
            IReadOnlyDictionary<RuleInfo, TypeRuleFactory<ScriptRule>> scriptRuleFactories)
            : base(scriptRuleFactories)
        {
        }
    }
}
