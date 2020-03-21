using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public interface IScriptAnalyzerFactory
    {
        ScriptAnalyzer Create();
    }

    public abstract class ScriptAnalyzerFactory : IScriptAnalyzerFactory
    {
        protected abstract IRuleProvider GetRuleProvider();

        protected abstract IRuleExecutorFactory GetExecutorFactory();

        public ScriptAnalyzer Create()
        {
            return new ScriptAnalyzer(GetRuleProvider(), GetExecutorFactory());
        }
    }
}
