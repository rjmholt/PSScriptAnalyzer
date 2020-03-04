using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class CompositeRuleFactory : IRuleFactory
    {
        private readonly IReadOnlyList<IRuleFactory> _ruleProviders;

        public CompositeRuleFactory(IReadOnlyList<IRuleFactory> ruleProviders)
        {
            _ruleProviders = ruleProviders;
        }

        public IEnumerable<IAstRule> GetAstRules()
        {
            var rules = new List<IAstRule>();
            foreach (IRuleFactory ruleProvider in _ruleProviders)
            {
                rules.AddRange(ruleProvider.GetAstRules());
            }
            return rules;
        }

        public IEnumerable<ITokenRule> GetTokenRules()
        {
            var rules = new List<ITokenRule>();
            foreach (IRuleFactory ruleProvider in _ruleProviders)
            {
                rules.AddRange(ruleProvider.GetTokenRules());
            }
            return rules;
        }
    }
}
