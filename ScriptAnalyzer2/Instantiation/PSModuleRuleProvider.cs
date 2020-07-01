using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System.Collections.Generic;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    internal class PSModuleRuleProvider : IRuleProvider
    {
        private readonly IReadOnlyList<PSCommandRule> _rules;

        public PSModuleRuleProvider(IReadOnlyList<PSCommandRule> rules)
        {
            _rules = rules;
        }

        public IEnumerable<RuleInfo> GetRuleInfos()
        {
            foreach (PSCommandRule rule in _rules)
            {
                yield return rule.RuleInfo;
            }
        }

        public IEnumerable<ScriptRule> GetScriptRules()
        {
            return _rules;
        }

        public void ReturnRule(Rule rule)
        {
            // No implementation required
        }
    }
}
