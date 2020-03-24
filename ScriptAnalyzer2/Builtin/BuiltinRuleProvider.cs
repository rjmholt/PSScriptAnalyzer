using Microsoft.PowerShell.ScriptAnalyzer.Builder;
using Microsoft.PowerShell.ScriptAnalyzer.Builtin.Rules;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration.InMemory;
using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using Microsoft.PowerShell.ScriptAnalyzer.Runtime;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.BuiltinRules;
using System;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;

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

        public static BuiltinRuleProvider Create(
            IRuleConfigurationCollection ruleConfigurationCollection,
            IRuleComponentProvider ruleComponentProvider)
        {
            return new BuiltinRuleProvider(TypeRuleProvider.GetRuleFactoriesFromTypes(ruleConfigurationCollection, ruleComponentProvider, s_defaultRules));
        }

        private BuiltinRuleProvider(
            IReadOnlyDictionary<RuleInfo, TypeRuleFactory<ScriptRule>> scriptRuleFactories)
            : base(scriptRuleFactories)
        {
        }
    }

    public static class Default
    {
        private static readonly Lazy<IRuleComponentProvider> s_ruleComponentProviderLazy = new Lazy<IRuleComponentProvider>(BuildRuleComponentProvider);

        public static IRuleConfigurationCollection RuleConfiguration { get; } = new MemoryRuleConfigurationCollection(new Dictionary<string, IRuleConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            { "PS/AvoidUsingEmptyCatchBlock", null },
            { "PS/AvoidGlobalVars", null },
            { "PS/AvoidUsingPositionalParameters", null },
            { "PS/AvoidUsingWMICmdlet", null },
            { "PS/UseDeclaredVarsMoreThanAssignments", null },
            { "PS/UseShouldProcessForStateChangingFunctions", null },
        });

        public static IRuleExecutorFactory RuleExecutorFactory { get; } = new ParallelLinqRuleExecutorFactory();

        public static IRuleComponentProvider RuleComponentProvider => s_ruleComponentProviderLazy.Value;

        private static IRuleComponentProvider BuildRuleComponentProvider()
        {
            return new RuleComponentProviderBuilder()
                .AddSingleton(InstantiatePowerShellCommandDatabase())
                .Build();
        }

        private static IPowerShellCommandDatabase InstantiatePowerShellCommandDatabase()
        {
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();
                return SessionStateCommandDatabase.Create(runspace.SessionStateProxy.InvokeCommand);
            }
        }
    }

}
