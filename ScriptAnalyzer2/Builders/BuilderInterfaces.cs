using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builder
{
    public interface INeedsConfigurationStep<TBuilder>
    {
        TBuilder WithPsdConfiguration(string input);

        TBuilder WithJsonConfiguration(string input);

        TBuilder WithConfigurationFile(string configurationPath);
    }
}
