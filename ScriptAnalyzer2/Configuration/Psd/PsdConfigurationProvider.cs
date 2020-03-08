using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd
{
    public abstract class PsdConfigurationProvider : IConfigurationProvider
    {
        private readonly PsdTypedObjectConverter _psdConverter;

        protected PsdConfigurationProvider()
        {
            _psdConverter = new PsdTypedObjectConverter();
        }

        public IScriptAnalyzerConfiguration GetScriptAnalyzerConfiguration()
        {
            var configuration = _psdConverter.Convert<IReadOnlyDictionary<string, ExpressionAst>>(GetConfigurationAst());
            var ruleNames = _psdConverter.Convert<IReadOnlyList<string>>(configuration["RuleNames"]);
            var ruleConfigurations = _psdConverter.Convert<IReadOnlyDictionary<string, HashtableAst>>(configuration["Rules"]);

            return new PsdScriptAnalyzerConfiguration(_psdConverter, ruleNames, ruleConfigurations);
        }

        protected abstract HashtableAst GetConfigurationAst();
    }

    public class PsdFileConfigurationProvider : PsdConfigurationProvider
    {
        private readonly string _psdFilePath;

        public PsdFileConfigurationProvider(string psdFilePath)
        {
            _psdFilePath = psdFilePath;
        }

        protected override HashtableAst GetConfigurationAst()
        {
            Ast ast = Parser.ParseFile(_psdFilePath, out Token[] _, out ParseError[] parseErrors);
            return (HashtableAst)((CommandExpressionAst)((PipelineAst)((ScriptBlockAst)ast).EndBlock.Statements[0]).PipelineElements[0]).Expression;
        }
    }

    public class PsdStringConfigurationProvider : PsdConfigurationProvider
    {
        private readonly string _psdContent;

        public PsdStringConfigurationProvider(string psdContent)
        {
            _psdContent = psdContent;
        }

        protected override HashtableAst GetConfigurationAst()
        {
            Ast ast = Parser.ParseInput(_psdContent, out Token[] _, out ParseError[] parseErrors);
            return (HashtableAst)((CommandExpressionAst)((PipelineAst)((ScriptBlockAst)ast).EndBlock.Statements[0]).PipelineElements[0]).Expression;
        }
    }
}
