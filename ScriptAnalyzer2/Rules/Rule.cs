using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer.Rules
{
    public interface IResettable
    {
        void Reset();
    }

    public abstract class Rule
    {
        protected Rule(RuleInfo ruleInfo)
        {
            RuleInfo = ruleInfo;
        }

        public RuleInfo RuleInfo { get; }
    }

    public abstract class Rule<TConfiguration> : Rule where TConfiguration : IRuleConfiguration
    {
        protected Rule(RuleInfo ruleInfo, TConfiguration ruleConfiguration)
            : base(ruleInfo)
        {
            Configuration = ruleConfiguration;
        }

        public TConfiguration Configuration { get; }
    }

    public abstract class ScriptRule : Rule
    {
        protected ScriptRule(RuleInfo ruleInfo) : base(ruleInfo)
        {
        }

        public abstract IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast ast, IReadOnlyList<Token> tokens, string scriptPath);
    }

    public abstract class ScriptRule<TConfiguration> : Rule<TConfiguration> where TConfiguration : IRuleConfiguration
    {
        protected ScriptRule(RuleInfo ruleInfo, TConfiguration ruleConfiguration) : base(ruleInfo, ruleConfiguration)
        {
        }

        public abstract IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast ast, IReadOnlyList<Token> tokens, string scriptPath);
    }
}
