using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptAnalyzer
    {
        private readonly IRuleProvider _ruleProvider;

        public ScriptAnalyzer(IRuleProvider ruleProvider)
        {
            _ruleProvider = ruleProvider;
        }

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScriptPath(string path)
        {
            Ast ast = Parser.ParseFile(path, out Token[] tokens, out ParseError[] parseErrors);
            return AnalyzeScript(ast, tokens, path);
        }

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScriptInput(string input)
        {
            Ast ast = Parser.ParseInput(input, out Token[] tokens, out ParseError[] parseErrors);
            return AnalyzeScript(ast, tokens, scriptPath: null);
        }

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast scriptAst, Token[] scriptTokens) =>
            AnalyzeScript(scriptAst, scriptTokens, scriptPath: null);

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast scriptAst, Token[] scriptTokens, string scriptPath)
        {
            var diagnostics = new List<ScriptDiagnostic>();

            foreach (ScriptRule scriptRule in _ruleProvider.GetScriptRules())
            {
                try
                {
                    diagnostics.AddRange(scriptRule.AnalyzeScript(scriptAst, scriptTokens, scriptPath));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error executing rule {scriptRule.RuleInfo.Name}:\n{e}");
                }
            }

            return diagnostics;
        }
    }
}
