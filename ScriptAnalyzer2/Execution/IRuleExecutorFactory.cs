using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Execution
{
    interface IRuleExecutorFactory
    {
        IRuleExecutor CreateRuleExecutor();
    }
}
