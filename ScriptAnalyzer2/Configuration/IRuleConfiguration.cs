using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration
{
    public class CommonConfiguration
    {
        public CommonConfiguration(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Enabled { get; }
    }

    public interface IRuleConfiguration
    {
        CommonConfiguration Common { get; }

        TConfiguration AsConfigurationObject<TConfiguration>() where TConfiguration : IRuleConfiguration;
    }
}
