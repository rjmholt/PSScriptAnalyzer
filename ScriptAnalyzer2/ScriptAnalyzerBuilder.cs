using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptAnalyzerBuilder
    {
        private readonly List<IRuleFactory> _ruleProviders;

        public ScriptAnalyzerBuilder()
        {
            _ruleProviders = new List<IRuleFactory>();
        }

        public ScriptAnalyzerBuilder AddRuleAssembly(string assemblyPath)
        {
            _ruleProviders.Add(AssemblyRuleFactory.FromAssembly(assemblyPath));
            return this;
        }

        public ScriptAnalyzerBuilder AddRuleAssembly(Assembly assembly)
        {
            _ruleProviders.Add(AssemblyRuleFactory.FromAssembly(assembly));
            return this;
        }

        public ScriptAnalyzer Build()
        {
            IRuleFactory ruleProvider = _ruleProviders.Count == 1
                ? _ruleProviders[0]
                : new CompositeRuleFactory(_ruleProviders);

            return new ScriptAnalyzer(new AstAnalyzer(ruleProvider));
        }
    }
}
