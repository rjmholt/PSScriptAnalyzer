using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Xml.Linq;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;

namespace Engine.XmlAnalysis
{
    /// <summary>
    /// Analyzes a .cdxml file for PowerShell usage
    /// </summary>
    public class CdXmlAnalyzer : XmlAnalyzer
    {
        /// <summary>
        /// Construct a new CdXmlAnalyzer.
        /// </summary>
        /// <param name="ps1XmlFilePath">the path of the XML file to analyze</param>
        /// <param name="scriptAnalyzer">the script analyzer to refer analysis back to</param>
        /// <param name="useCompatibleTypesRule">the UseCompatibleTypes rule for type name analysis</param>
        public CdXmlAnalyzer(string ps1XmlFilePath, ScriptAnalyzer scriptAnalyzer, IScriptRule useCompatibleTypesRule)
            : base(ps1XmlFilePath, scriptAnalyzer, useCompatibleTypesRule)
        {
        }

        /// <summary>
        /// Load and analyze the Xml file given to this analyzer.
        /// </summary>
        /// <returns>diagnostic records from XML PowerShell analysis</returns>
        public override IEnumerable<DiagnosticRecord> AnalyzeXml()
        {
            XDocument cdXmlDocument = XDocument.Load(XmlFilePath);
            return AnalyzeXmlNode(cdXmlDocument.Root);
        }

        /// <summary>
        /// Recursively analyze XML nodes in the document. In CDXML, we only
        /// look for "PSType" attributes to analyze, since that's the only place
        /// we can depend on bad types.
        /// </summary>
        /// <param name="currentElement">the XML element we have just read and wish to analyze</param>
        /// <returns>diagnostic records containing the analysis of this and all child nodes</returns>
        private IEnumerable<DiagnosticRecord> AnalyzeXmlNode(XElement currentElement)
        {
            IScriptExtent currentExtent = UpdatePosition(currentElement);

            // Look for <Type PSType="<typename>"> nodes and check the typename for compatibility
            if (currentElement.Name.LocalName == "Type")
            {
                // TODO: Case insensitive
                XAttribute psTypeAttr = currentElement.Attribute("PSType");
                if (psTypeAttr != null)
                {
                    TypeExpressionAst fakeTypeAst = GenerateFakeTypeAstFromTypeName(psTypeAttr.Value, currentExtent);
                    foreach (DiagnosticRecord dr in UseCompatibleTypesRule.AnalyzeScript(fakeTypeAst, XmlFilePath))
                    {
                        yield return dr;
                    }
                }
            }

            // If no children are present, stop here
            if (!currentElement.HasElements)
            {
                yield break;
            }

            // Recursively examine children
            foreach (XElement childNode in currentElement.Elements())
            {
                foreach (DiagnosticRecord dr in AnalyzeXmlNode(childNode))
                {
                    yield return dr;
                }
            }
        }
    }
}