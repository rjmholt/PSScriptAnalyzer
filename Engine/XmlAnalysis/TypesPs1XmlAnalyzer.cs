using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;

namespace Engine.XmlAnalysis
{
    /// <summary>
    /// Class for PowerShell analysis of .types.ps1xml files
    /// </summary>
    public class TypesPs1XmlAnalyzer : XmlAnalyzer
    {
        /// <summary>
        /// Create a new .types.ps1xml analyzer object
        /// </summary>
        /// <param name="ps1XmlFilePath">the path of the XML file to analyze</param>
        /// <param name="scriptAnalyzer">the script analyzer to perform PowerShell analysis with</param>
        /// <param name="useCompatibleTypesRule">the UseCompatibleTypes rule, to run on directly embedded type names</param>
        public TypesPs1XmlAnalyzer(string ps1XmlFilePath, ScriptAnalyzer scriptAnalyzer, IScriptRule useCompatibleTypesRule) 
            : base(ps1XmlFilePath, scriptAnalyzer, useCompatibleTypesRule)
        {
        }

        /// <summary>
        /// Analyze the XML document this object holds
        /// </summary>
        /// <returns>diagnostic records with analysis for the XML document</returns>
        public override IEnumerable<DiagnosticRecord> AnalyzeXml()
        {
            // TODO possibly resolve a URI here
            XDocument typesPs1XmlDoc = XDocument.Load(XmlFilePath, LoadOptions.SetLineInfo);
            return AnalyzeTypesPs1XmlNode(typesPs1XmlDoc.Root);
        }

        /// <summary>
        /// XML tags that contain script in .types.ps1xml
        /// </summary>
        private static readonly HashSet<string> s_typesScriptXmlTags = new HashSet<string>()
        {
            "GetScriptBlock",
            "Script",
            "ScriptBlock",
        };

        /// <summary>
        /// Recursively analyze an XML document element
        /// </summary>
        /// <param name="currentElement">the XML element to be analyzed</param>
        /// <param name="parentIsType">true if the enclosing element was a &lt;Type&gt; tag</param>
        /// <returns>diagnostic records with analysis of this element and all its children</returns>
        private IEnumerable<DiagnosticRecord> AnalyzeTypesPs1XmlNode(XElement currentElement, bool parentIsType = false)
        {
            IScriptExtent currentExtent = UpdatePosition(currentElement);

            // Look for <Name>System.TypeName</Name> kinds of tags underneath <Type> tags
            if (parentIsType && currentElement.Name.LocalName == "Name" && !DescribesCimClass(currentElement.Value))
            {
                TypeExpressionAst fakeTypeAst = GenerateFakeTypeAstFromTypeName(currentElement.Value, currentExtent);
                foreach (DiagnosticRecord typeDR in UseCompatibleTypesRule.AnalyzeScript(fakeTypeAst, XmlFilePath))
                {
                    yield return typeDR;
                }
                // Name tags have no children of interest
                yield break;
            }

            // Look for tags that contain embedded scriptblocks
            if (s_typesScriptXmlTags.Contains(currentElement.Name.LocalName))
            {
                foreach (DiagnosticRecord dr in ScriptAnalyzer.AnalyzeScriptDefinition(currentElement.Value))
                {
                    yield return dr;
                }
            }
            // Look for embedded type references like <TypeName>System.TypeName</Name>
            else if (currentElement.Name == "TypeName" && !DescribesCimClass(currentElement.Value))
            {
                TypeExpressionAst fakeTypeAst = GenerateFakeTypeAstFromTypeName(currentElement.Value, currentExtent);
                foreach (DiagnosticRecord typeDR in UseCompatibleTypesRule.AnalyzeScript(fakeTypeAst, String.Empty))
                {
                    yield return typeDR;
                }
            }

            // If there are no children, we're done
            if (!currentElement.HasElements)
            {
                yield break;
            }

            // Recursively analyze child nodes
            foreach (XElement childNode in currentElement.Elements())
            {
                foreach (DiagnosticRecord childDR in AnalyzeTypesPs1XmlNode(childNode, parentIsType: currentElement.Name == "Type"))
                {
                    yield return childDR;
                }
            }
        }

    }
}