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
    public abstract class XmlAnalyzer
    {
        protected static readonly IScriptExtent s_emptyExtent = new ScriptExtent(
            new ScriptPosition("", 0, 0, ""),
            new ScriptPosition("", 0, 0, "")
        );

        protected static bool DescribesCimClass(string typeName)
        {
            return typeName.StartsWith("Microsoft.Management.Infrastructure.CimInstance#");
        }

        protected XmlAnalyzer(string ps1XmlFilePath, ScriptAnalyzer parentAnalyzer, IScriptRule useCompatibleTypesRule)
        {
            ScriptAnalyzer = parentAnalyzer;
            UseCompatibleTypesRule = useCompatibleTypesRule;
            XmlFilePath = ps1XmlFilePath;
            PreviousPosition = new ReaderPosition(ps1XmlFilePath, 0, 0);
            CurrentPosition = PreviousPosition;
            FileLines = File.ReadLines(ps1XmlFilePath).ToArray();
        }

        protected IScriptRule UseCompatibleTypesRule { get; }

        protected ScriptAnalyzer ScriptAnalyzer { get; }

        protected string XmlFilePath { get; }

        protected ReaderPosition CurrentPosition { get; set; }

        protected ReaderPosition PreviousPosition { get; set; }

        protected string[] FileLines { get; }

        public abstract IEnumerable<DiagnosticRecord> AnalyzeXml();

        protected IScriptExtent UpdatePosition(XElement currNode)
        {
            var nodeLineInfo = (IXmlLineInfo)currNode;
            PreviousPosition = CurrentPosition;
            CurrentPosition = new ReaderPosition(XmlFilePath, nodeLineInfo.LineNumber, nodeLineInfo.LinePosition);

            return GetCurrentExtent();
        }

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