using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Rules
{
    public interface IRule
    {
        string Name { get; }

        string Namespace { get; }

        string Description { get; }

        string SourcePath { get; }

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

}
