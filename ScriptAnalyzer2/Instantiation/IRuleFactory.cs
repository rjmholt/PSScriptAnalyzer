using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public interface IRuleFactory
    {
        IEnumerable<IAstRule> GetAstRules();

        IEnumerable<ITokenRule> GetTokenRules();
    }

}
