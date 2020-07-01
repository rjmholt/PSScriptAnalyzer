using Microsoft.PowerShell.ScriptAnalyzer.Builder;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using System.Management.Automation;

#if !CORECLR
using Microsoft.PowerShell.ScriptAnalyzer.Internal;
#endif

namespace Microsoft.PowerShell.ScriptAnalyzer.Commands
{
    [Cmdlet(VerbsLifecycle.Invoke, "ScriptAnalyzer")]
    public class InvokeScriptAnalyzerCommand : PSCmdlet
    {
        private ScriptAnalyzer _scriptAnalyzer;

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "FilePath")]
        public string[] Path { get; set; }

        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Input")]
        public string[] ScriptDefinition { get; set; }

        [Parameter]
        public string ConfigurationPath { get; set; }

        [Parameter]
        public string[] ExcludeRules { get; set; }

        protected override void BeginProcessing()
        {
            ConfigurationPath = GetUnresolvedProviderPathFromPSPath(ConfigurationPath);
            _scriptAnalyzer = CreateScriptAnalyzer();
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

        private ScriptAnalyzer CreateScriptAnalyzer()
        {
            var configBuilder = new ScriptAnalyzerConfigurationBuilder()
                .WithBuiltinRuleSet(BuiltinRulePreference.Default);

            if (ConfigurationPath != null)
            {
                configBuilder.AddConfigurationFile(ConfigurationPath);
            }

            return configBuilder.Build().CreateScriptAnalyzer();
        }
    }
}
