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

    public abstract class AstRule : Rule
    {
        protected AstRule(RuleInfo ruleInfo) : base(ruleInfo)
        {
        }

        public abstract IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast ast, string scriptPath);
    }

    public abstract class AstRule<TConfiguration> : AstRule where TConfiguration : IRuleConfiguration
    {
        protected AstRule(RuleInfo ruleInfo, TConfiguration ruleConfiguration)
            : base(ruleInfo)
        {
            Configuration = ruleConfiguration;
        }

        public TConfiguration Configuration { get; }
    }

    public abstract class TokenRule : Rule
    {
        protected TokenRule(RuleInfo ruleInfo) : base(ruleInfo)
        {
        }

        public abstract IReadOnlyList<ScriptDiagnostic> AnalyzeScript(IReadOnlyList<Token> tokens, string scriptPath);
    }

    public abstract class TokenRule<TConfiguration> : TokenRule where TConfiguration : IRuleConfiguration
    {
        protected TokenRule(RuleInfo ruleInfo, TConfiguration ruleConfiguration) : base(ruleInfo)
        {
            Configuration = ruleConfiguration;
        }

        public TConfiguration Configuration { get; }
    }
}
