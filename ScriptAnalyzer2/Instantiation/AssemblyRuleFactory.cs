using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class AssemblyRuleFactory : IRuleFactory
    {
        public static AssemblyRuleFactory FromAssembly(string assemblyPath)
        {
            return FromAssembly(Assembly.LoadFile(assemblyPath));
        }

        public static AssemblyRuleFactory FromAssembly(Assembly assembly)
        {
            var tokenRules = new Dictionary<string, TokenRuleGenerator>();
            var astRules = new Dictionary<string, AstRuleGenerator>();

            foreach (Type type in assembly.GetExportedTypes())
            {
                var ruleAttribute = type.GetCustomAttribute<RuleAttribute>();

                if (typeof(TokenRule).IsAssignableFrom(type))
                {
                }
            }
        }

        private AssemblyRuleFactory()
        {
        }

        public IEnumerable<AstRule> GetAstRules()
        {
        }

        public IEnumerable<TokenRule> GetTokenRules()
        {
        }
    }
}
