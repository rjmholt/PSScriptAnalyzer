namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration
{
    public interface IConfigurationProvider
    {
        IScriptAnalyzerConfiguration GetScriptAnalyzerConfiguration();
    }
}
