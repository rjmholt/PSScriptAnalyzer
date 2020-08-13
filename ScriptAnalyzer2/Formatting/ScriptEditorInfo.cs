using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Formatting
{
    public class ScriptEditorInfo
    {
        public ScriptEditorInfo(string name, string @namespace, string description)
        {
            Name = name;
            Namespace = @namespace;
            Description = description;
        }

        public string Name { get; }

        public string Namespace { get; }

        public string FullName { get; }

        public string Description { get; }
    }
}
