using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public IReadOnlyCollection<ScriptDiagnostic> AnalyzeScriptPath(string path)
        {
            Ast ast = Parser.ParseFile(path, out Token[] tokens, out ParseError[] parseErrors);
            return AnalyzeScript(ast, tokens, path);
        }

        public IReadOnlyCollection<ScriptDiagnostic> AnalyzeScriptInput(string input)
        {
            Ast ast = Parser.ParseInput(input, out Token[] tokens, out ParseError[] parseErrors);
            return AnalyzeScript(ast, tokens, scriptPath: null);
        }

        public IReadOnlyCollection<ScriptDiagnostic> AnalyzeScript(Ast scriptAst, Token[] scriptTokens) =>
            AnalyzeScript(scriptAst, scriptTokens, scriptPath: null);

        public IReadOnlyCollection<ScriptDiagnostic> AnalyzeScript(Ast scriptAst, Token[] scriptTokens, string scriptPath)
        {
            var ruleExecutor = new ParallelLinqRuleExecutor(scriptAst, scriptTokens, scriptPath);

            foreach (ScriptRule rule in _ruleProvider.GetScriptRules())
            {
                ruleExecutor.AddRule(rule);
            }

            return ruleExecutor.CollectDiagnostics();
        }
    }
}
