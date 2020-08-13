using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Language;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Formatting
{
    internal class SequentialStringBuilderFormatBuffer : IScriptFormatBuffer
    {
        private readonly StringBuilder _sb;

        private readonly List<ScriptEdit> _editList;

        public SequentialStringBuilderFormatBuffer(string scriptContent, Ast ast, IReadOnlyList<Token> tokens, string scriptFilePath)
        {
            OriginalScriptContent = scriptContent;
            OriginalAst = ast;
            OriginalTokens = tokens;
            ScriptFilePath = scriptFilePath;
            _sb = new StringBuilder(scriptContent.Length);
        }

        public string OriginalScriptContent { get; }

        public Ast OriginalAst { get; }

        public IReadOnlyList<Token> OriginalTokens { get; }

        public string ScriptFilePath { get; }

        public IScriptFormatBuffer Reparse()
        {
            string newScript = _sb.ToString();
            Ast ast = Parser.ParseInput(newScript, out Token[] tokens, out ParseError[] errors);
            return new SequentialStringBuilderFormatBuffer(newScript, ast, tokens, ScriptFilePath);
        }

        public void Replace(IScriptEditor editor, int startOffset, int endOffset, ReadOnlySpan<char> newValue)
        {
            _editList.Add(new ScriptEdit(editor, startOffset, endOffset, newValue.ToString()));
        }

        private readonly struct ScriptEdit
        {
            public ScriptEdit(IScriptEditor editor, int startOffset, int endOffset, string newValue)
            {
                Editor = editor;
                StartOffset = startOffset;
                EndOffset = endOffset;
                NewValue = newValue;
            }

            public readonly IScriptEditor Editor;

            public readonly int StartOffset;

            public readonly int EndOffset;

            public readonly string NewValue;
        }
    }
}
