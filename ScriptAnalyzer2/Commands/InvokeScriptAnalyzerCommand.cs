using Microsoft.PowerShell.ScriptAnalyzer.Builtin.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Commands
{
    [Cmdlet(VerbsLifecycle.Invoke, "ScriptAnalyzer2")]
    public class InvokeScriptAnalyzerCommand : Cmdlet
    {
        private static ConcurrentDictionary<string, ScriptAnalyzer> _configuredScriptAnalyzers = new ConcurrentDictionary<string, ScriptAnalyzer>();

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "FilePath")]
        public string[] Path { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Input")]
        public string[] ScriptDefinition { get; set; }

        [Parameter]
        public string ConfigurationPath { get; set; }

        private ScriptAnalyzer _scriptAnalyzer;

        protected override void BeginProcessing()
        {
            _scriptAnalyzer = GetScriptAnalyzer();
        }

        protected override void ProcessRecord()
        {
            if (Path != null)
            {
                foreach (string path in Path)
                {
                    foreach (ScriptDiagnostic diagnostic in _scriptAnalyzer.AnalyzeScriptPath(path))
                    {
                        WriteObject(diagnostic);
                    }
                }

                return;
            }

            if (ScriptDefinition != null)
            {
                foreach (string input in ScriptDefinition)
                {
                    foreach (ScriptDiagnostic diagnostic in _scriptAnalyzer.AnalyzeScriptInput(input))
                    {
                        WriteObject(diagnostic);
                    }
                }
            }
        }

        private ScriptAnalyzer GetScriptAnalyzer()
        {
            string configurationPath = ConfigurationPath ?? string.Empty;

            return _configuredScriptAnalyzers.GetOrAdd(configurationPath, (path) =>
            {
                var builder = ConfigurationPath != null
                    ? RuleProviderBuilder.FromConfigurationFile(ConfigurationPath)
                    : new RuleProviderBuilder(BuiltinScriptAnalyzerConfiguration.Instance);

                builder.AddBuiltinRules();

                return new ScriptAnalyzer(builder.Build());
            });
        }
    }
}
