using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptAnalyzer
    {
        private readonly AstAnalyzer _astAnalyzer;

        public ScriptAnalyzer(AstAnalyzer astAnalyzer)
        {
            _astAnalyzer = astAnalyzer;
        }

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScriptPath(string path)
        {
            Ast ast = Parser.ParseFile(path, out Token[] tokens, out ParseError[] parseErrors);
            return AnalyzeScript(ast, tokens);
        }

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScriptInput(string input)
        {
            Ast ast = Parser.ParseInput(input, out Token[] tokens, out ParseError[] parseErrors);
            return AnalyzeScript(ast, tokens);
        }

        private IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast ast, Token[] tokens)
        {
            return _astAnalyzer.AnalyzeScript(ast, tokens);
        }
    }
}
