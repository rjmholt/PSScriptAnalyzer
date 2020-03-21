﻿using Microsoft.PowerShell.ScriptAnalyzer.Builder;
using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptAnalyzer
    {
        private readonly IRuleProvider _ruleProvider;

        private readonly IRuleExecutorFactory _executorFactory;

        public ScriptAnalyzer(
            IRuleProvider ruleProvider,
            IRuleExecutorFactory executorFactory)
        {
            _ruleProvider = ruleProvider;
            _executorFactory = executorFactory;
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
            IRuleExecutor ruleExecutor = _executorFactory.CreateRuleExecutor();

            foreach (ScriptRule rule in _ruleProvider.GetScriptRules())
            {
                ruleExecutor.AddRule(rule);
            }

            return ruleExecutor.CollectDiagnostics();
        }
    }
}
