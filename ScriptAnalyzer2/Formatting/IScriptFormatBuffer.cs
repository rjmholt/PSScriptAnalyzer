using System;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer.Formatting
{
    public interface IScriptFormatBuffer
    {
        string OriginalScriptContent { get; }

        Ast OriginalAst { get; }

        IReadOnlyList<Token> OriginalTokens { get; }

        string ScriptFilePath { get; }

        void Replace(IScriptEditor editor, int startOffset, int endOffset, ReadOnlySpan<char> newValue);
    }

    public static class ScriptFormatBufferExtensions
    {
        private static readonly char[] s_allNewlineStarts = new[] { '\r', '\n' };

        public static void Replace(this IScriptFormatBuffer formatStream, IScriptEditor editor, IScriptExtent extent, ReadOnlySpan<char> newValue)
            => formatStream.Replace(editor, extent.StartOffset, extent.EndOffset, newValue);

        public static void Replace(this IScriptFormatBuffer formatStream, IScriptEditor editor, Ast ast, ReadOnlySpan<char> newValue)
            => formatStream.Replace(editor, ast.Extent, newValue);

        public static void Replace(this IScriptFormatBuffer formatStream, IScriptEditor editor, Token token, ReadOnlySpan<char> newValue)
            => formatStream.Replace(editor, token.Extent, newValue);

        public static void Replace(this IScriptFormatBuffer formatStream, IScriptEditor editor, IScriptPosition startPosition, IScriptPosition endPosition, ReadOnlySpan<char> newValue)
            => formatStream.Replace(editor, startPosition.Offset, endPosition.Offset, newValue);

        public static void Replace(this IScriptFormatBuffer formatStream, IScriptEditor editor, Ast startAst, Ast endAst, ReadOnlySpan<char> newValue)
            => formatStream.Replace(editor, startAst.Extent.StartOffset, endAst.Extent.EndOffset, newValue);

        public static void Replace(this IScriptFormatBuffer formatStream, IScriptEditor editor, Token startToken, Token endToken, ReadOnlySpan<char> newValue)
            => formatStream.Replace(editor, startToken.Extent.StartOffset, startToken.Extent.EndOffset, newValue);

        public static void Replace(this IScriptFormatBuffer formatStream, IScriptEditor editor, int startLine, int startColumn, int endLine, int endColumn, ReadOnlySpan<char> newValue)
            => formatStream.Replace(editor, startLine, startColumn, endLine, endColumn, NewlineType.All, newValue);

        public static void Replace(this IScriptFormatBuffer formatStream, IScriptEditor editor, int startLine, int startColumn, int endLine, int endColumn, NewlineType newlineTypes, ReadOnlySpan<char> newValue)
        {
            (int startOffset, int endOffset) = formatStream.GetOffsets(startLine, startColumn, endLine, endColumn, newlineTypes);
            formatStream.Replace(editor, startOffset, endOffset, newValue);
        }

        private static (int, int) GetOffsets(this IScriptFormatBuffer formatStream, int startLine, int startColumn, int endLine, int endColumn, NewlineType newlineTypes)
        {
            switch (newlineTypes)
            {
                case NewlineType.Environment:
                    return formatStream.GetOffsetsForSingleNewline(startLine, startColumn, endLine, endColumn, Environment.NewLine);

                case NewlineType.LF:
                    return formatStream.GetOffsetsForSingleNewline(startLine, startColumn, endLine, endColumn, "\n");

                case NewlineType.CRLF:
                    return formatStream.GetOffsetsForSingleNewline(startLine, startColumn, endLine, endColumn, "\r\n");

                case NewlineType.All:
                    return formatStream.GetOffsetsForAllNewlines(startLine, startColumn, endLine, endColumn);

                default:
                    throw new ArgumentException($"Unknown value for {nameof(newlineTypes)}: {newlineTypes}");
            }
        }

        private static (int, int) GetOffsetsForSingleNewline(this IScriptFormatBuffer formatStream, int startLine, int startColumn, int endLine, int endColumn, string newline)
        {
            int currentOffset = 0;
            int currentLine = 0;

            while (currentLine < startLine)
            {
                currentOffset = formatStream.OriginalScriptContent.IndexOf(newline, currentOffset + 1);
                currentLine++;
            }

            int startOffset = currentOffset + startColumn - 1;

            while (currentLine < endLine)
            {
                currentOffset = formatStream.OriginalScriptContent.IndexOf(newline, currentOffset + 1);
                currentLine++;
            }

            int endOffset = currentOffset + endColumn - 1;

            return (startOffset, endOffset);
        }

        private static (int, int) GetOffsetsForAllNewlines(this IScriptFormatBuffer formatStream, int startLine, int startColumn, int endLine, int endColumn)
        {
            int currentOffset = 0;
            int currentLine = 0;

            while (currentLine < startLine)
            {
                currentOffset = formatStream.OriginalScriptContent.IndexOfAny(s_allNewlineStarts, currentOffset + 1);

                if (formatStream.OriginalScriptContent[currentOffset] == '\r'
                    && (currentOffset + 1 >= formatStream.OriginalScriptContent.Length
                        || formatStream.OriginalScriptContent[currentOffset + 1] != '\n'))
                {
                    continue;
                }

                currentLine++;
            }

            int startOffset = currentOffset + startColumn - 1;

            while (currentLine < endLine)
            {
                currentOffset = formatStream.OriginalScriptContent.IndexOfAny(s_allNewlineStarts, currentOffset + 1);

                if (formatStream.OriginalScriptContent[currentOffset] == '\r'
                    && (currentOffset + 1 >= formatStream.OriginalScriptContent.Length
                        || formatStream.OriginalScriptContent[currentOffset + 1] != '\n'))
                {
                    continue;
                }

                currentLine++;
            }

            int endOffset = currentOffset + endColumn - 1;

            return (startOffset, endOffset);
        }
    }
}
