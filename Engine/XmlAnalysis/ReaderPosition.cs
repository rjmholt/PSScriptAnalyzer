using System.Management.Automation.Language;

namespace Engine.XmlAnalysis
{
    public struct ReaderPosition
    {
        public ReaderPosition(string filePath, int lineNumber, int columnNumber)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public string FilePath { get; }

        public int LineNumber { get; set; }

        public int ColumnNumber { get; set; }
    }
}