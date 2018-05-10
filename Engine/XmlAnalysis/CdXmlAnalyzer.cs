using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Xml.Linq;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;

namespace Engine.XmlAnalysis
{
    public class CdXmlAnalyzer : XmlAnalyzer
    {
        public CdXmlAnalyzer(string ps1XmlFilePath, ScriptAnalyzer parentAnalyzer, IScriptRule useCompatibleTypesRule)
            : base(ps1XmlFilePath, parentAnalyzer, useCompatibleTypesRule)
        {
        }

        public override IEnumerable<DiagnosticRecord> AnalyzeXml()
        {
            XDocument cdXmlDocument = XDocument.Load(XmlFilePath);
            return AnalyzeXmlNode(cdXmlDocument.Root);
        }

        private IEnumerable<DiagnosticRecord> AnalyzeXmlNode(XElement currNode)
        {
            IScriptExtent currentExtent = UpdatePosition(currNode);

            if (currNode.Name.LocalName == "Type")
            {
                XAttribute psTypeAttr = currNode.Attribute("PSType");
                if (psTypeAttr != null)
                {
                    TypeExpressionAst fakeTypeAst = new TypeExpressionAst(currentExtent, new TypeName(currentExtent, psTypeAttr.Value));
                    foreach (DiagnosticRecord dr in UseCompatibleTypesRule.AnalyzeScript(fakeTypeAst, XmlFilePath))
                    {
                        yield return dr;
                    }
                }
            }

            if (!currNode.HasElements)
            {
                yield break;
            }

            foreach (XElement childNode in currNode.Elements())
            {
                foreach (DiagnosticRecord dr in AnalyzeXmlNode(childNode))
                {
                    yield return dr;
                }
            }
        }
    }
}