
using System.Data;

namespace Microsoft.PowerShell.ScriptAnalyzer.Rules
{
    public class RuleInfo
    {
        public RuleInfo FromRuleAttribute(RuleAttribute ruleAttribute, string description)
        {
            return new RuleInfo(
                ruleAttribute.Name,
                ruleAttribute.Namespace,
                description,
                ruleAttribute.Severity,
                ruleAttribute.IsThreadsafe);
        }

        public RuleInfo(
            string name,
            string @namespace,
            string description,
            DiagnosticSeverity severity,
            bool isThreadsafe)
        {
            Name = name;
            Namespace = @namespace;
            Fullname = $"{@namespace}/{name}";
            Description = description;
            Severity = severity;
            IsThreadsafe = isThreadsafe;
        }

        public string Name { get; }

        public string Namespace { get; }

        public string Fullname { get; }

        public string Description { get; }

        public DiagnosticSeverity Severity { get; }

        public bool IsThreadsafe { get; }
    }
}
