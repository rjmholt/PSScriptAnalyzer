using System;

namespace Microsoft.PowerShell.ScriptAnalyzer.Rules
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RuleAttribute : Attribute
    {
        public RuleAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public DiagnosticSeverity Severity { get; set; } = DiagnosticSeverity.Warning;

        public string Description { get; set; }

        public string Namespace { get; set; }

        public bool IsThreadsafe { get; set; } = false;

        public bool IsIdempotent { get; set; } = false;
    }
}
