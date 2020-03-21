using Microsoft.PowerShell.ScriptAnalyzer.Configuration.Json;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration
{
    public class FileConfigurationProvider
    {
        public IScriptAnalyzerConfiguration GetConfiguration(string filePath)
        {
            if (string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase))
            {
                return JsonScriptAnalyzerConfiguration.FromFile(filePath);
            }

            // Assume any file extension that isn't ".json" is PSD
            return PsdScriptAnalyzerConfiguration.FromFile(filePath);
        }
    }
}
