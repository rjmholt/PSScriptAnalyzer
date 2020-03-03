using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptPosition : IScriptPosition
    {
        public ScriptPosition FromOffset(string scriptText, string scriptPath, int offset)
        {
            int currLine = 1;
            int i = 0;
            int lastLineOffset = -1;
            while (i < offset)
            {
                lastLineOffset = i;
                i = scriptText.IndexOf('\n', i);
                currLine++;
            }

            return new ScriptPosition(scriptText, scriptPath, scriptText.Substring(lastLineOffset, offset), offset, currLine, offset - lastLineOffset);
        }

        public ScriptPosition FromPosition(string scriptText, string scriptPath, int line, int column)
        {
            int offset = 0;
            int currLine = 1;
            while (currLine < line)
            {
                offset = scriptText.IndexOf('\n', offset);
                currLine++;
            }

            string lineText = scriptText.Substring(offset, offset + column - 1);
            offset += column - 1;

            return new ScriptPosition(scriptText, scriptPath, lineText, offset, line, column);
        }

        private readonly string _scriptText;

        public ScriptPosition(string scriptText, string scriptPath, string line, int offset, int lineNumber, int columnNumber)
        {
            _scriptText = scriptText;
            File = scriptPath;
            Line = line;
            Offset = offset;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public int ColumnNumber { get; }

        public string File { get; }

        public string Line { get; }

        public int LineNumber { get; }

        public int Offset { get; }

        public string GetFullScript() => _scriptText;
    }

    public class ScriptExtent : IScriptExtent
    {
        public ScriptExtent(string text, IScriptPosition start, IScriptPosition end)
        {
            StartScriptPosition = start;
            EndScriptPosition = end;
            Text = text;
        }

        public int EndColumnNumber => EndScriptPosition.ColumnNumber;

        public int EndLineNumber => EndScriptPosition.LineNumber;

        public int EndOffset => EndScriptPosition.Offset;

        public IScriptPosition EndScriptPosition { get; }

        public string File => StartScriptPosition.File;

        public int StartColumnNumber => StartScriptPosition.ColumnNumber;

        public int StartLineNumber => StartScriptPosition.LineNumber;

        public int StartOffset => StartScriptPosition.Offset;

        public IScriptPosition StartScriptPosition { get; }

        public string Text { get; }
    }

    public class Correction
    {
        internal Correction FromLegacyCorrection(CorrectionExtent legacyCorrection)
        {
        }

        public Correction(IScriptExtent extent, string correctionText, string description)
        {
            Extent = extent;
            CorrectionText = correctionText;
        }

        public IScriptExtent Extent { get; }

        public string CorrectionText { get; }

        public string Description { get; }
    }

    public class AstCorrection : Correction
    {
        public AstCorrection(Ast correctedAst, string correctionText, string description)
            : base(correctedAst.Extent, correctionText, description)
        {
            Ast = correctedAst;
        }

        public Ast Ast { get; }
    }

    public class TokenCorrection : Correction
    {
        public TokenCorrection(Token correctedToken, string correctionText, string description)
            : base(correctedToken.Extent, correctionText, description)
        {
            Token = correctedToken;
        }

        public Token Token { get; }
    }

    public class ScriptDiagnostic
    {
        public ScriptDiagnostic(string message, IScriptExtent scriptExtent, DiagnosticSeverity severity)
        {
            Message = message;
            ScriptExtent = scriptExtent;
            Severity = severity;
        }

        public string Message { get; }

        public IScriptExtent ScriptExtent { get; }

        public DiagnosticSeverity Severity { get; }
    }

    public class ScriptAstDiagnostic : ScriptDiagnostic
    {
        public ScriptAstDiagnostic(string message, Ast ast, DiagnosticSeverity severity)
            : base(message, ast.Extent, severity)
        {
            Ast = ast;
        }

        public Ast Ast { get; }
    }

    public class ScriptTokenDiagnostic : ScriptDiagnostic
    {
        public ScriptTokenDiagnostic(string message, Token token, DiagnosticSeverity severity)
            : base(message, token.Extent, severity)
        {
            Token = token;
        }

        public Token Token { get; }
    }

    public interface IRule
    {
        string Name { get; }

        string Namespace { get; }

        string Description { get; }

        string SourcePath { get; }

        SourceType SourceType { get; }

        DiagnosticSeverity Severity { get; }
    }

    public interface IAstRule : IRule
    {
        IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast ast, string scriptPath);
    }

    public interface ITokenRule : IRule
    {
        IReadOnlyList<ScriptDiagnostic> AnalyzeScript(IReadOnlyList<Token> tokens, string scriptPath);
    }

    internal class LegacyScriptRuleAdapter : IAstRule
    {
        private readonly IScriptRule _rule;

        public LegacyScriptRuleAdapter(IScriptRule rule)
        {
            _rule = rule;
        }

        public string Name => _rule.GetName();

        public string Namespace => _rule.GetSourceName();

        public string Description => _rule.GetDescription();

        public string SourcePath => null;

        public SourceType SourceType => _rule.GetSourceType();

        public DiagnosticSeverity Severity => (DiagnosticSeverity)_rule.GetSeverity();

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast ast, string scriptPath)
        {
            var diagnostics = new List<ScriptDiagnostic>();
            foreach (DiagnosticRecord legacyDiagnostic in _rule.AnalyzeScript(ast, scriptPath))
            {
                diagnostics.Add(new ScriptDiagnostic(legacyDiagnostic.Message, legacyDiagnostic.Extent, legacyDiagnostic.Severity));
            }
            return diagnostics;
        }
    }

    internal class LegacyTokenRuleAdapter : ITokenRule
    {
        private readonly Windows.PowerShell.ScriptAnalyzer.Generic.ITokenRule _rule;

        public LegacyTokenRuleAdapter(Windows.PowerShell.ScriptAnalyzer.Generic.ITokenRule rule)
        {
            _rule = rule;
        }

        public string Name => _rule.GetName();

        public string Namespace => _rule.GetSourceName();

        public string Description => _rule.GetDescription();

        public string SourcePath => null;

        public SourceType SourceType => _rule.GetSourceType();

        public DiagnosticSeverity Severity => (DiagnosticSeverity)_rule.GetSeverity();

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScript(IReadOnlyList<Token> tokens, string scriptPath)
        {
            var tokenArray = new Token[tokens.Count];
            for (int i = 0; i < tokens.Count; i++)
            {
                tokenArray[i] = tokens[i];
            }

            var diagnostics = new List<ScriptDiagnostic>();
            foreach (DiagnosticRecord legacyDiagnostic in _rule.AnalyzeTokens(tokenArray, scriptPath))
            {
                diagnostics.Add(new ScriptDiagnostic(legacyDiagnostic.Message, legacyDiagnostic.Extent, legacyDiagnostic.Severity));
            }
            return diagnostics;
        }
    }

    public interface IRuleProvider
    {
        IEnumerable<IAstRule> GetAstRules();

        IEnumerable<ITokenRule> GetTokenRules();
    }

    public class ScriptAnalyzer2
    {
        private readonly AstAnalyzer _astAnalyzer;

        public ScriptAnalyzer2(AstAnalyzer astAnalyzer)
        {
            _astAnalyzer = astAnalyzer;
        }

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScriptPath(string path)
        {
            Ast ast = Parser.ParseFile(path, out Token[] tokens, out ParseError[] parseErrors);
            return _astAnalyzer.AnalyzeScript(ast, tokens);
        }

        public IReadOnlyList<ScriptDiagnostic> AnalyzeScriptInput(string input)
        {
            Ast ast = Parser.ParseInput(input, out Token[] tokens, out ParseError[] parseErrors);
            return _astAnalyzer.AnalyzeScript(ast, tokens);
        }
    }

    public class AstAnalyzer
    {
        private readonly IRuleProvider _ruleProvider;

        internal AstAnalyzer(IRuleProvider ruleProvider)
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
                diagnostics.AddRange(scriptRule.AnalyzeScript(scriptAst, scriptPath));
            }

            foreach (ITokenRule tokenRule in _ruleProvider.GetTokenRules())
            {
                diagnostics.AddRange(tokenRule.AnalyzeScript(scriptTokens, scriptPath));
            }

            return diagnostics;
        }
    }
}
