using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;

namespace Engine.XmlAnalysis
{
    /// <summary>
    /// Base class for analyzing PowerShell-containing XML files
    /// </summary>
    public abstract class XmlAnalyzer
    {

        /// <summary>
        /// Checks whether a type signature refers to a CIM class
        /// </summary>
        /// <param name="typeName">the type name to check</param>
        /// <returns>true if the type name refers to a CIM class, false otherwise</returns>
        protected static bool DescribesCimClass(string typeName)
        {
            return typeName.StartsWith("Microsoft.Management.Infrastructure.CimInstance#");
        }

        /// <summary>
        /// Generates a fake type expression AST to run analysis on from a string
        /// representing a .NET type name
        /// </summary>
        /// <param name="typeName">the full type name to run analysis on</param>
        /// <returns>a spoofed AST that script analysis can work with</returns>
        protected static TypeExpressionAst GenerateFakeTypeAstFromTypeName(string typeName, IScriptExtent currentExtent)
        {
            // TODO: We can generate better ITypeName objects by parsing the typename properly
            // but it might not be worth it and the analyzer might not care
            return new TypeExpressionAst(currentExtent, new TypeName(currentExtent, typeName));
        }

        /// <summary>
        /// Create a new XmlAnalyzer object
        /// </summary>
        /// <param name="ps1XmlFilePath">the path to the file to analyze</param>
        /// <param name="scriptAnalyzer">the script analyzer object using this one</param>
        /// <param name="useCompatibleTypesRule">the rule to check type compatibility in type names embedded in XML</param>
        protected XmlAnalyzer(string ps1XmlFilePath, ScriptAnalyzer scriptAnalyzer, IScriptRule useCompatibleTypesRule)
        {
            ScriptAnalyzer = scriptAnalyzer;
            // TODO: Rather than pass this in, make the analyzer accept general ASTs
            UseCompatibleTypesRule = useCompatibleTypesRule;
            XmlFilePath = ps1XmlFilePath;
            PreviousPosition = new ReaderPosition(ps1XmlFilePath, 0, 0);
            CurrentPosition = PreviousPosition;
            // TODO: Don't do IO in the constructor -- make a factory method
            FileLines = File.ReadLines(ps1XmlFilePath).ToArray();
        }

        /// <summary>
        /// Specific rule for checking type compatibility, used for type names directly embedded in XML
        /// </summary>
        protected IScriptRule UseCompatibleTypesRule { get; }

        /// <summary>
        /// The script analyzer instance we're using, needed to run analysis
        /// </summary>
        /// <returns></returns>
        protected ScriptAnalyzer ScriptAnalyzer { get; }

        /// <summary>
        /// The path to the XML file
        /// </summary>
        protected string XmlFilePath { get; }

        /// <summary>
        /// The current position of the XML reader
        /// </summary>
        /// <returns></returns>
        protected ReaderPosition CurrentPosition { get; set; }

        /// <summary>
        /// The position of the XML reader when it started reading the last XML element
        /// </summary>
        protected ReaderPosition PreviousPosition { get; set; }

        /// <summary>
        /// Lines of the file, memoized so we can use them to report extents
        /// </summary>
        protected string[] FileLines { get; }

        /// <summary>
        /// The XML analysis hook the analyzer will call
        /// </summary>
        public abstract IEnumerable<DiagnosticRecord> AnalyzeXml();

        /// <summary>
        /// Update the XmlAnalyzer's current and last positions, and return the new current extent
        /// </summary>
        /// <param name="currentElement">the current element we have just read</param>
        protected IScriptExtent UpdatePosition(XElement currentElement)
        {
            var nodeLineInfo = (IXmlLineInfo)currentElement;
            PreviousPosition = CurrentPosition;
            CurrentPosition = new ReaderPosition(XmlFilePath, nodeLineInfo.LineNumber, nodeLineInfo.LinePosition);

            return GetCurrentExtent();
        }

        /// <summary>
        /// Generate a new extent object based on the current position of
        /// the XmlAnalyzer
        /// </summary>
        private IScriptExtent GetCurrentExtent()
        {
            return new ScriptExtent(
                new ScriptPosition(
                    XmlFilePath,
                    PreviousPosition.LineNumber,
                    PreviousPosition.ColumnNumber,
                    FileLines[PreviousPosition.LineNumber]
                ),
                new ScriptPosition(
                    XmlFilePath,
                    CurrentPosition.LineNumber,
                    CurrentPosition.ColumnNumber,
                    FileLines[CurrentPosition.LineNumber]
                )
            );
        }
    }
}