using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Windows.PowerShell.ScriptAnalyzer;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;

namespace Engine.XmlAnalysis
{
    public class TypesPs1XmlAnalyzer : XmlAnalyzer
    {
        public TypesPs1XmlAnalyzer(string ps1XmlFilePath, ScriptAnalyzer parentAnalyzer, IScriptRule useCompatibleTypesRule) 
            : base(ps1XmlFilePath, parentAnalyzer, useCompatibleTypesRule)
        {
        }

        public override IEnumerable<DiagnosticRecord> AnalyzeXml()
        {
            // TODO possibly resolve a URI here
            XDocument typesPs1XmlDoc = XDocument.Load(XmlFilePath, LoadOptions.SetLineInfo);
            return AnalyzeTypesPs1XmlNode(typesPs1XmlDoc.Root);
        }

        private static readonly HashSet<string> s_typesScriptXmlTags = new HashSet<string>()
        {
            "GetScriptBlock",
            "Script",
            "ScriptBlock",
        };

        private IEnumerable<DiagnosticRecord> AnalyzeTypesPs1XmlNode(XElement currNode, bool parentIsType = false)
        {
            IScriptExtent currExtent = UpdatePosition(currNode);

            if (parentIsType && currNode.Name.LocalName == "Name" && !DescribesCimClass(currNode.Value))
            {
                TypeExpressionAst fakeTypeAst = new TypeExpressionAst(currExtent, new TypeName(currExtent, currNode.Value));
                foreach (DiagnosticRecord typeDR in UseCompatibleTypesRule.AnalyzeScript(fakeTypeAst, XmlFilePath))
                {
                    yield return typeDR;
                }
                yield break;
            }

            if (s_typesScriptXmlTags.Contains(currNode.Name.LocalName))
            {
                foreach (DiagnosticRecord dr in ScriptAnalyzer.AnalyzeScriptDefinition(currNode.Value))
                {
                    yield return dr;
                }
            }
            else if (currNode.Name == "TypeName" && !DescribesCimClass(currNode.Value))
            {
                TypeExpressionAst fakeTypeAst = new TypeExpressionAst(currExtent, new TypeName(currExtent, currNode.Value));
                foreach (DiagnosticRecord typeDR in UseCompatibleTypesRule.AnalyzeScript(fakeTypeAst, String.Empty))
                {
                    yield return typeDR;
                }
            }

            if (!currNode.HasElements)
            {
                yield break;
            }

            foreach (XElement childNode in currNode.Elements())
            {
                foreach (DiagnosticRecord childDR in AnalyzeTypesPs1XmlNode(childNode, parentIsType: currNode.Name == "Type"))
                {
                    yield return childDR;
                }
            }
        }

    }
}