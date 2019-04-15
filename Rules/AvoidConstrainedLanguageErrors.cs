using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;

namespace Microsoft.Windows.PowerShell.ScriptAnalyzer.BuiltinRules
{
    public class AvoidConstrainedLanguageErrors : ConfigurableRule
    {
        public override IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string fileName)
        {
            var visitor = new ConstrainedLanguageAnalysisVisitor(fileName, rule: this);
            ast.Visit(visitor);
            return visitor.GetDiagnostics();
        }

        public override string GetCommonName()
        {
            return GetName();
        }

        public override string GetDescription()
        {
            return "Warns about PowerShell uses that may cause Constrained Language mode to fail";
        }

        public override string GetName()
        {
            return "AvoidConstrainedLanguageMode";
        }

        public override RuleSeverity GetSeverity()
        {
            return RuleSeverity.Warning;
        }

        public DiagnosticSeverity GetDiagnosticSeverity()
        {
            return DiagnosticSeverity.Warning;
        }

        public override string GetSourceName()
        {
            return "PS";
        }

        public override SourceType GetSourceType()
        {
            return SourceType.Builtin;
        }

#if (PSv3 || PSv4)
        private class ConstrainedLanguageAnalysisVisitor : AstVisitor
#else
        private class ConstrainedLanguageAnalysisVisitor : AstVisitor2
#endif
        {
            private static readonly ISet<string> s_badCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "New-JobTrigger",
                "Add-JobTrigger",
                "Remove-JobTrigger",
                "Get-JobTrigger",
                "Set-JobTrigger",
                "Enable-JobTrigger",
                "Disable-JobTrigger",
                "New-ScheduledJobOption",
                "Get-ScheduledJobOption",
                "Set-ScheduledJobOption",
                "Register-ScheduledJob",
                "Get-ScheduledJob",
                "Set-ScheduledJob",
                "Unregister-ScheduledJob",
                "Enable-ScheduledJob",
                "Disable-ScheduledJob",
                "Add-Type",
            };

            private readonly string _analyzedFilePath;

            private readonly AvoidConstrainedLanguageErrors _rule;

            private readonly List<DiagnosticRecord> _diagnostics;

            public ConstrainedLanguageAnalysisVisitor(string analyzedFilePath, AvoidConstrainedLanguageErrors rule)
            {
                _analyzedFilePath = analyzedFilePath;
                _rule = rule;
                _diagnostics = new List<DiagnosticRecord>();
            }

            public IEnumerable<DiagnosticRecord> GetDiagnostics()
            {
                return _diagnostics;
            }

            public override AstVisitAction VisitCommand(CommandAst commandAst)
            {
                if (commandAst.InvocationOperator == TokenKind.Dot)
                {
                    AddDiagnostic(
                        "Dot-sourcing may not work in Constrained Language mode. Ensure the target of dot-sourcing is also in Constrained Language Mode",
                        commandAst.Extent);
                }

                string commandName = commandAst.GetCommandName();

                if (string.IsNullOrEmpty(commandName))
                {
                    return AstVisitAction.Continue;
                }

                if (commandName.Equals("New-Object", StringComparison.OrdinalIgnoreCase))
                {
                    CheckNewObject(commandAst);
                    return AstVisitAction.Continue;
                }

                if (s_badCommands.Contains(commandName))
                {
                    AddDiagnostic(
                        $"The command {commandName} is not allowed in Constrained Language Mode",
                        commandAst.Extent);
                    return AstVisitAction.Continue;
                }

                if (commandName.Equals("Start-Job", StringComparison.OrdinalIgnoreCase))
                {
                    AddDiagnostic(
                        "Start-Job will not work in Constrained Language Mode if the system is not locked down",
                        commandAst.Extent);
                    return AstVisitAction.Continue;
                }

                if (commandName.Equals("Set-PSBreakpoint", StringComparison.OrdinalIgnoreCase))
                {
                    AddDiagnostic(
                        "Set-PSBreakpoint may not work in Constrained Language Mode. It requires system-wide lockdown through UMCI",
                        commandAst.Extent);
                    return AstVisitAction.Continue;
                }

                if (commandName.Equals("Import-LocalizedData", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (CommandElementAst commandElement in commandAst.CommandElements)
                    {
                        if (!(commandElement is CommandParameterAst parameterAst))
                        {
                            continue;
                        }

                        if (parameterAst.ParameterName.Equals("SupportedCommand", StringComparison.OrdinalIgnoreCase))
                        {
                            AddDiagnostic(
                                "The -SupportedCommand of the Import-LocalizedData cmdlet is not allowed in Constrained Language Mode",
                                commandAst.Extent);
                        }
                    }

                    return AstVisitAction.Continue;
                }

                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst)
            {
                AddDiagnostic(
                    "Type conversion is not allowed in Constrained Language mode",
                    typeConstraintAst.Extent);

                return AstVisitAction.Continue;
            }

#if !(PSv3 || PSv4)
            public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst)
            {
                AddDiagnostic(
                    "DSC Configurations are not allowed in Constrained Language mode",
                    configurationDefinitionAst.Extent);

                return AstVisitAction.Continue;
            }


            public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
            {
                if (!typeDefinitionAst.IsClass)
                {
                    return AstVisitAction.SkipChildren;
                }

                AddDiagnostic(
                    "Classes cannot be used in Constrained Language Mode",
                    typeDefinitionAst.Extent);

                return AstVisitAction.SkipChildren;
            }
#endif

            private void CheckNewObject(CommandAst commandAst)
            {
                foreach (CommandElementAst commandElement in commandAst.CommandElements)
                {
                    if (!(commandElement is CommandParameterAst parameterAst))
                    {
                        continue;
                    }

                    string parameterName = parameterAst.ParameterName;
                    if (parameterName.Equals("ComObject", StringComparison.OrdinalIgnoreCase)
                        || parameterName.Equals("Strict", StringComparison.OrdinalIgnoreCase))
                    {
                        AddDiagnostic(
                            "COM object creation is not allowed in Constrained Language Mode",
                            commandAst.Extent);
                        return;
                    }
                }
            }

            private void AddDiagnostic(string message, IScriptExtent extent)
            {
                _diagnostics.Add(CreateDiagnostic(message, extent));
            }

            private DiagnosticRecord CreateDiagnostic(string message, IScriptExtent extent)
            {
                return new DiagnosticRecord(
                    message,
                    extent,
                    _rule.GetName(),
                    _rule.GetDiagnosticSeverity(),
                    _analyzedFilePath);
            }
        }
    }
}