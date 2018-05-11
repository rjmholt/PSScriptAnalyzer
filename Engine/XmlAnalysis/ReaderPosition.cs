using System.Management.Automation.Language;

namespace Engine.XmlAnalysis
{
    /// <summary>
    /// Describes a position of the XmlAnalyzer "head" (where it's read to)
    /// </summary>
    public struct ReaderPosition
    {
        /// <summary>
        /// Create a new reader position descriptor.
        /// </summary>
        /// <param name="filePath">the path of the XML file being read</param>
        /// <param name="lineNumber">the horizontal line location in the file of the reader</param>
        /// <param name="columnNumber">the vertical column (line offset) in the file</param>
        public ReaderPosition(string filePath, int lineNumber, int columnNumber)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        /// <summary>
        /// The path of the XML file read.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The horiztonal line number of the reader "head".
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The vertical column (line offset) of the reader "head".
        /// </summary>
        public int ColumnNumber { get; set; }
    }
}