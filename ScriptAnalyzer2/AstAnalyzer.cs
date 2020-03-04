using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class AstAnalyzer
    {
        private readonly IRuleFactory _ruleProvider;

        internal AstAnalyzer(IRuleFactory ruleProvider)
        {
            _ruleProvider = ruleProvider;
        }

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast scriptAst, Token[] scriptTokens) =>
            AnalyzeScript(scriptAst, scriptTokens, scriptPath: null);

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast scriptAst, Token[] scriptTokens, string scriptPath)
        {
            var diagnostics = new List<ScriptDiagnostic>();

            foreach (IAstRule scriptRule in _ruleProvider.GetAstRules())
            {
                try
                {
                    diagnostics.AddRange(scriptRule.AnalyzeScript(scriptAst, scriptPath));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error executing rule {scriptRule.Name}:\n{e}");
                }
            }

            foreach (ITokenRule tokenRule in _ruleProvider.GetTokenRules())
            {
                try
                {
                    diagnostics.AddRange(tokenRule.AnalyzeScript(scriptTokens, scriptPath));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error executing rule {tokenRule.Name}:\n{e}");
                }
            }

            return diagnostics;
        }
    }

}
