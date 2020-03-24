using Microsoft.PowerShell.ScriptAnalyzer.Builder;
using Microsoft.PowerShell.ScriptAnalyzer.Builtin.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
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
        private static readonly ConcurrentDictionary<ParameterSetting, ScriptAnalyzer> s_configuredScriptAnalyzers = new ConcurrentDictionary<ParameterSetting, ScriptAnalyzer>();

        private static readonly FileConfigurationProvider s_fileConfigurationProvider = new FileConfigurationProvider();

        private ScriptAnalyzer _scriptAnalyzer;

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "FilePath")]
        public string[] Path { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Input")]
        public string[] ScriptDefinition { get; set; }

        [Parameter]
        public string ConfigurationPath { get; set; }

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
            var parameters = new ParameterSetting(this);
            return s_configuredScriptAnalyzers.GetOrAdd(parameters, CreateScriptAnalyzerWithParameters));
        }

        private ScriptAnalyzer CreateScriptAnalyzerWithParameters(ParameterSetting parameters)
        {
            var scriptAnalyzerBuilder = new ScriptAnalyzerBuilder();

            if (string.IsNullOrEmpty(parameters.ConfigurationPath))
            {
                scriptAnalyzerBuilder = scriptAnalyzerBuilder.AddConfigurationFile(parameters.ConfigurationPath);
            }

            return scriptAnalyzerBuilder.Build();
        }

        private struct ParameterSetting
        {
            public ParameterSetting(InvokeScriptAnalyzerCommand command)
            {
                ConfigurationPath = command.ConfigurationPath;
            }

            public string ConfigurationPath { get; }

            public override int GetHashCode()
            {
                return HashCode.Combine(ConfigurationPath);
            }
        }
    }
}
