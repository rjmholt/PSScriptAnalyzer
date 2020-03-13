using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public interface IRuleProvider
    {
        IEnumerable<AstRule> GetAstRules();

        IEnumerable<TokenRule> GetTokenRules();

        void ReturnRule(Rule rule);
    }
}
