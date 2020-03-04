using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptDiagnostic
    {
        public ScriptDiagnostic(
            string message,
            IScriptExtent scriptExtent,
            DiagnosticSeverity severity)
            : this(message, scriptExtent, severity, corrections: null)
        {
        }

        public ScriptDiagnostic(
            string message,
            IScriptExtent scriptExtent,
            DiagnosticSeverity severity,
            IReadOnlyList<Correction> corrections)
        {
            Corrections = corrections;
            Message = message;
            ScriptExtent = scriptExtent;
            Severity = severity;
        }

        public string Message { get; }

        public IScriptExtent ScriptExtent { get; }

        public DiagnosticSeverity Severity { get; }

        public IReadOnlyList<Correction> Corrections { get; }
    }

    public class ScriptAstDiagnostic : ScriptDiagnostic
    {
        public ScriptAstDiagnostic(string message, Ast ast, DiagnosticSeverity severity)
            : this(message, ast, severity, corrections: null)
        {
        }

        public ScriptAstDiagnostic(
            string message,
            Ast ast,
            DiagnosticSeverity severity,
            IReadOnlyList<Correction> corrections)
            : base(message, ast.Extent, severity, corrections)
        {
            Ast = ast;
        }


        public Ast Ast { get; }
    }

    public class ScriptTokenDiagnostic : ScriptDiagnostic
    {
        public ScriptTokenDiagnostic(string message, Token token, DiagnosticSeverity severity)
            : this(message, token, severity, corrections: null)
        {
        }

        public ScriptTokenDiagnostic(string message, Token token, DiagnosticSeverity severity, IReadOnlyList<Correction> corrections)
            : base(message, token.Extent, severity, corrections)
        {
            Token = token;
        }

        public Token Token { get; }
    }

}
