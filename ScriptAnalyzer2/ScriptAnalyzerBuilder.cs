using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptAnalyzerBuilder
    {
        private readonly List<IRuleProvider> _ruleProviders;

        public ScriptAnalyzerBuilder()
        {
            _ruleProviders = new List<IRuleProvider>();
        }

        public ScriptAnalyzerBuilder AddRuleAssembly(string assemblyPath)
        {
            _ruleProviders.Add(AssemblyRuleProvider.FromAssembly(assemblyPath));
            return this;
        }

        public ScriptAnalyzerBuilder AddRuleAssembly(Assembly assembly)
        {
            _ruleProviders.Add(AssemblyRuleProvider.FromAssembly(assembly));
            return this;
        }

        public ScriptAnalyzer Build()
        {
            IRuleProvider ruleProvider = _ruleProviders.Count == 1
                ? _ruleProviders[0]
                : new CompositeRuleProvider(_ruleProviders);

            return new ScriptAnalyzer(new AstAnalyzer(ruleProvider));
        }
    }
}
