
using System;
using System.Data;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Rules
{
    public class RuleInfo
    {
        public static bool TryGetFromRuleType(Type ruleType, out RuleInfo ruleInfo)
        {
            var ruleAttr = ruleType.GetCustomAttribute<RuleAttribute>();

            if (ruleAttr == null)
            {
                ruleInfo = null;
                return false;
            }

            RuleDescriptionAttribute ruleDescriptionAttr = ruleType.GetCustomAttribute<RuleDescriptionAttribute>();

            string ruleNamespace = ruleAttr.Namespace
                ?? ruleType.Assembly.GetCustomAttribute<RuleCollectionAttribute>()?.Name
                ?? ruleType.Assembly.GetName().Name;

            ruleInfo = new RuleInfo(
                ruleAttr.Name,
                ruleNamespace,
                ruleDescriptionAttr?.Description,
                ruleAttr.Severity,
                ruleAttr.IsThreadsafe,
                ruleAttr.IsIdempotent);
            return true;
        }

        private RuleInfo(
            string name,
            string @namespace,
            string description,
            DiagnosticSeverity severity,
            bool isThreadsafe,
            bool isIdempotent)
        {
            Name = name;
            Namespace = @namespace;
            Fullname = $"{@namespace}/{name}";
            Description = description;
            Severity = severity;
            IsThreadsafe = isThreadsafe;
            IsIdempotent = isIdempotent;
        }

        public string Name { get; }

        public string Namespace { get; }

        public string Fullname { get; }

        public string Description { get; }

        public DiagnosticSeverity Severity { get; }

        public bool IsThreadsafe { get; }

        public bool IsIdempotent { get; }
    }
}
