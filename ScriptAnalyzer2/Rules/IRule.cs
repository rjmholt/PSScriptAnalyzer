using System.Collections.Generic;
using System.Management.Automation.Language;

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

    public interface IRule<TConfiguration> : IRule
    {
    }

    public interface IAstRule : IRule
    {
        IReadOnlyList<ScriptDiagnostic> AnalyzeScript(Ast ast, string scriptPath);
    }

    public interface IAstRule<TConfiguration> : IAstRule
    {
    }

    public interface ITokenRule : IRule
    {
        IReadOnlyList<ScriptDiagnostic> AnalyzeScript(IReadOnlyList<Token> tokens, string scriptPath);
    }

    public interface ITokenRule<TConfiguration> : ITokenRule
    {
    }
}
