using Microsoft.PowerShell.ScriptAnalyzer.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Formatting
{
    public class ScriptEditorAttribute : ComponentDefinitionAttribute
    {
        public ScriptEditorAttribute(string name, string description)
            : base(name, description)
        {
        }

        public ScriptEditorAttribute(string name, Type descriptionResourceProvider, string descriptionResourceKey)
            : base(name, descriptionResourceProvider, descriptionResourceKey)
        {
        }
    }
}
