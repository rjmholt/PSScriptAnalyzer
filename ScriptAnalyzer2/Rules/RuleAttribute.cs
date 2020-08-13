using Microsoft.PowerShell.ScriptAnalyzer.Common;
using System;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Rules
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RuleAttribute : ComponentDefinitionAttribute
    {
        public RuleAttribute(string name, string description)
            : base(name, description)
        {
        }

        public RuleAttribute(string name, Type descriptionResourceProvider, string descriptionResourceKey)
            : base(name, descriptionResourceProvider, descriptionResourceKey)
        {
        }

        public DiagnosticSeverity Severity { get; set; } = DiagnosticSeverity.Warning;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ThreadsafeRuleAttribute : ScriptAnalyzerAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class IdempotentRuleAttribute : ScriptAnalyzerAttribute
    {
    }
}
