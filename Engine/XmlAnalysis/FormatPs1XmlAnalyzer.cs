using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Xml.Linq;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;

namespace Engine.XmlAnalysis
{
    public class FormatPs1XmlAnalyzer : XmlAnalyzer
    {
        public FormatPs1XmlAnalyzer(string ps1XmlFilePath, ScriptAnalyzer parentAnalyzer, IScriptRule useCompatibleTypesRule)
            : base(ps1XmlFilePath, parentAnalyzer, useCompatibleTypesRule)
        {
        }

        public override IEnumerable<DiagnosticRecord> AnalyzeXml()
        {
            XDocument formatPs1XmlDoc = XDocument.Load(XmlFilePath);
            return AnalyzeFormatPs1XmlNode(formatPs1XmlDoc.Root);
        }

        private IEnumerable<DiagnosticRecord> AnalyzeFormatPs1XmlNode(XElement currNode)
        {
            IScriptExtent currExtent = UpdatePosition(currNode);

            if (currNode.Name.LocalName == "TypeName" && !DescribesCimClass(currNode.Value))
            {
                TypeExpressionAst fakeTypeAst = new TypeExpressionAst(currExtent, new TypeName(currExtent, currNode.Value));
                foreach (DiagnosticRecord dr in UseCompatibleTypesRule.AnalyzeScript(fakeTypeAst, XmlFilePath))
                {
                    yield return dr;
                }
            }
            else if (currNode.Name.LocalName == "ScriptBlock")
            {
                foreach (DiagnosticRecord dr in ScriptAnalyzer.AnalyzeScriptDefinition(currNode.Value))
                {
                    yield return dr;
                }
            }

            if (!currNode.HasElements)
            {
                yield break;
            }

            foreach (XElement childNode in currNode.Elements())
            {
                foreach (DiagnosticRecord dr in AnalyzeFormatPs1XmlNode(childNode))
                {
                    yield return dr;
                }
            }
        }
    }
}