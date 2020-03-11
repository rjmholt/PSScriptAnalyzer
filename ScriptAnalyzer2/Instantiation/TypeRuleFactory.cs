using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public abstract class TypeRuleFactory<TRule>
    {
        public TypeRuleFactory(RuleInfo ruleInfo)
        {
            RuleInfo = ruleInfo;
        }

        public RuleInfo RuleInfo { get; }

        public abstract TRule GetRuleInstance();
    }

    public class AstTypeRuleFactory : TypeRuleFactory<AstRule>
    {
        private readonly Type _ruleType;

        public AstTypeRuleFactory(RuleInfo ruleInfo, Type ruleType)
            : base(ruleInfo)
        {
            _ruleType = ruleType;
        }

        public override AstRule GetRuleInstance()
        {
        }
    }

    public class TokenTypeRuleFactory : TypeRuleFactory<TokenRule>
    {
    }
}
