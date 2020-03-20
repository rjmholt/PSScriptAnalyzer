using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    interface IScriptAnalyzerComponentProvider
    {
        RuleComponentProvider RuleComponentProvider { get; }

        IRuleExecutorFactory RuleExecutorFactory { get; }
    }
}
