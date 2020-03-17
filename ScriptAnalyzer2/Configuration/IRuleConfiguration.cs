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
    }

    public class RuleConfiguration : IRuleConfiguration
    {
        public RuleConfiguration(CommonConfiguration common)
        {
            Common = common;
        }

        public CommonConfiguration Common { get; }
    }
}
