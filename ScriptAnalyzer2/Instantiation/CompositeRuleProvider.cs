using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class CompositeRuleProvider : IRuleProvider
    {
        private readonly IReadOnlyList<IRuleProvider> _ruleProviders;

        public CompositeRuleProvider(IReadOnlyList<IRuleProvider> ruleProviders)
        {
            _ruleProviders = ruleProviders;
        }

        public IEnumerable<IAstRule> GetAstRules()
        {
            var rules = new List<IAstRule>();
            foreach (IRuleProvider ruleProvider in _ruleProviders)
            {
                rules.AddRange(ruleProvider.GetAstRules());
            }
            return rules;
        }

        public IEnumerable<ITokenRule> GetTokenRules()
        {
            var rules = new List<ITokenRule>();
            foreach (IRuleProvider ruleProvider in _ruleProviders)
            {
                rules.AddRange(ruleProvider.GetTokenRules());
            }
            return rules;
        }
    }
}
