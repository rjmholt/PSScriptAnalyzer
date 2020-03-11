using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class CompositeRuleFactory : IRuleProvider
    {
        private readonly IReadOnlyList<IRuleProvider> _ruleProviders;

        public CompositeRuleFactory(IReadOnlyList<IRuleProvider> ruleProviders)
        {
            _ruleProviders = ruleProviders;
        }

        public IEnumerable<AstRule> GetAstRules()
        {
            var rules = new List<AstRule>();
            foreach (IRuleProvider ruleProvider in _ruleProviders)
            {
                rules.AddRange(ruleProvider.GetAstRules());
            }
            return rules;
        }

        public IEnumerable<TokenRule> GetTokenRules()
        {
            var rules = new List<TokenRule>();
            foreach (IRuleProvider ruleProvider in _ruleProviders)
            {
                rules.AddRange(ruleProvider.GetTokenRules());
            }
            return rules;
        }
    }
}
