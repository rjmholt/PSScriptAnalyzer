using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Language;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Formatting
{
    public class ScriptFormatter
    {
        public class Builder
        {
            private readonly List<IScriptEditor> _editors;

            public Builder()
            {
                _editors = new List<IScriptEditor>();
            }

            public Builder AddEditor(IScriptEditor editor)
            {
                _editors.Add(editor);
                return this;
            }

            public ScriptFormatter Build()
            {
                return new ScriptFormatter(_editors);
            }
        }

        private readonly IReadOnlyList<IScriptEditor> _editors;

        private ScriptFormatter(IReadOnlyList<IScriptEditor> editors)
        {
            _editors = editors;
        }

        public string FormatInput(string script)
        {
            SequentialStringBuilderFormatBuffer.FromInput
        }

        public void FormatFile(string inputScriptFilePath, string outputScriptFilePath)
        {
        }

        public void FormatFile(string scriptPath) => FormatFile(inputScriptFilePath: scriptPath, outputScriptFilePath: scriptPath);
    }
}
