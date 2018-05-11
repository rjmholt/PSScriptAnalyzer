using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Xml.Linq;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;

namespace Engine.XmlAnalysis
{
    /// <summary>
    /// Class to analyze PowerShell usage in format.ps1xml files
    /// </summary>
    public class FormatPs1XmlAnalyzer : XmlAnalyzer
    {
        /// <summary>
        /// Create a new format.ps1xml analyzer
        /// </summary>
        /// <param name="ps1XmlFilePath">the path of the XML file to analyze</param>
        /// <param name="scriptAnalyzer">the script analyzer to perform the PowerShell analysis</param>
        /// <param name="useCompatibleTypesRule">
        /// the UseCompatibleTypes rule to analyze the compatibility of XML-embedded types
        /// </param>
        public FormatPs1XmlAnalyzer(string ps1XmlFilePath, ScriptAnalyzer scriptAnalyzer, IScriptRule useCompatibleTypesRule)
            : base(ps1XmlFilePath, scriptAnalyzer, useCompatibleTypesRule)
        {
        }

        /// <summary>
        /// Analyze the XML file given to this analyzer for PowerShell diagnostics
        /// </summary>
        /// <returns>diagnostic records for any analysis of PowerShell usage or types in the XML file</returns>
        public override IEnumerable<DiagnosticRecord> AnalyzeXml()
        {
            XDocument formatPs1XmlDoc = XDocument.Load(XmlFilePath);
            return AnalyzeFormatPs1XmlNode(formatPs1XmlDoc.Root);
        }

        /// <summary>
        /// Recursively analyze an XML element using the script analyzer
        /// </summary>
        /// <param name="currentElement">the XML element in the document just read to be analyzed</param>
        /// <returns>diagnostic records with analysis of this element and all of its children</returns>
        private IEnumerable<DiagnosticRecord> AnalyzeFormatPs1XmlNode(XElement currentElement)
        {
            IScriptExtent currentExtent = UpdatePosition(currentElement);

            // Look for <TypeName>System.SomeTime</TypeName>
            if (currentElement.Name.LocalName == "TypeName" && !DescribesCimClass(currentElement.Value))
            {
                TypeExpressionAst fakeTypeAst = GenerateFakeTypeAstFromTypeName(currentElement.Value, currentExtent);
                foreach (DiagnosticRecord dr in UseCompatibleTypesRule.AnalyzeScript(fakeTypeAst, XmlFilePath))
                {
                    yield return dr;
                }
            }
            // Look for scripts in <ScriptBlock>$x = 1; $x + 8</ScriptBlock>
            else if (currentElement.Name.LocalName == "ScriptBlock")
            {
                foreach (DiagnosticRecord dr in ScriptAnalyzer.AnalyzeScriptDefinition(currentElement.Value))
                {
                    yield return dr;
                }
            }

            // Stop here if there are no children
            if (!currentElement.HasElements)
            {
                yield break;
            }

            // Recurse through the child elements of the node
            foreach (XElement childNode in currentElement.Elements())
            {
                foreach (DiagnosticRecord dr in AnalyzeFormatPs1XmlNode(childNode))
                {
                    yield return dr;
                }
            }
        }
    }
}