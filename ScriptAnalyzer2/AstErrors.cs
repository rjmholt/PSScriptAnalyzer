using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptAnalyzerException : Exception
    {
        protected ScriptAnalyzerException() : base()
        {
        }

        public ScriptAnalyzerException(string message) : base(message)
        {
        }

        public ScriptAnalyzerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ScriptAnalyzerConfigurationException : ScriptAnalyzerException
    {
        public ScriptAnalyzerConfigurationException() : base()
        {
        }

        public ScriptAnalyzerConfigurationException(string message) : base(message)
        {
        }

        public ScriptAnalyzerConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ConfigurationNotFoundException : ScriptAnalyzerConfigurationException
    {
        public ConfigurationNotFoundException() : base()
        {
        }

        public ConfigurationNotFoundException(string message) : base(message)
        {
        }
    }

    public class PowerShellParseErrorException : ScriptAnalyzerException
    {
        public PowerShellParseErrorException(string message, Ast parsedAst, IReadOnlyList<ParseError> parseErrors)
            : base(message)
        {
            ParsedAst = parsedAst;
            ParseErrors = parseErrors;
        }

        public Ast ParsedAst { get; }

        public IReadOnlyList<ParseError> ParseErrors { get; }
    }

    public class InvalidPowerShellExpressionException : ScriptAnalyzerException
    {
        public InvalidPowerShellExpressionException(string message)
            : base(message)
        {
        }
    }

    internal static class PowerShellParsing
    {
        public static HashtableAst ParseHashtableFromInput(string input)
        {
            ExpressionAst ast = ParseExpressionFromInput(input);

            if (!(ast is HashtableAst hashtableAst))
            {
                throw new InvalidPowerShellExpressionException($"Expression '{ast.Extent.Text}' was expected to be a hashtable");
            }

            return hashtableAst;
        }

        public static HashtableAst ParseHashtableFromFile(string filePath)
        {
            ExpressionAst ast = ParseExpressionFromFile(filePath);

            if (!(ast is HashtableAst hashtableAst))
            {
                throw new InvalidPowerShellExpressionException($"Expression '{ast.Extent.Text}' was expected to be a hashtable");
            }

            return hashtableAst;
        }

        public static ExpressionAst ParseExpressionFromInput(string input)
        {
            Ast ast = Parser.ParseInput(input, out Token[] _, out ParseError[] errors);

            if (errors != null && errors.Length > 0)
            {
                throw new PowerShellParseErrorException("Unable to parse input", ast, errors);
            }

            return GetExpressionAstFromScriptAst(ast);
        }

        public static ExpressionAst ParseExpressionFromFile(string filePath)
        {
            Ast ast = Parser.ParseFile(filePath, out Token[] _, out ParseError[] errors);

            if (errors != null && errors.Length > 0)
            {
                throw new PowerShellParseErrorException("Unable to parse input", ast, errors);
            }

            return GetExpressionAstFromScriptAst(ast);
        }

        internal static ExpressionAst GetExpressionAstFromScriptAst(Ast ast)
        {
            var scriptBlockAst = (ScriptBlockAst)ast;

            if (scriptBlockAst.EndBlock == null)
            {
                throw new InvalidPowerShellExpressionException("Expected 'end' block in PowerShell input");
            }

            if (scriptBlockAst.EndBlock.Statements == null
                || scriptBlockAst.EndBlock.Statements.Count == 0)
            {
                throw new InvalidPowerShellExpressionException("No statements to parse expression from in input");
            }

            if (scriptBlockAst.EndBlock.Statements.Count != 1)
            {
                throw new InvalidPowerShellExpressionException("Expected a single expression in input");
            }

            if (!(scriptBlockAst.EndBlock.Statements[0] is PipelineAst pipelineAst))
            {
                throw new InvalidPowerShellExpressionException($"Statement '{scriptBlockAst.EndBlock.Statements[0].Extent.Text}' is not a valid expression");
            }

            if (pipelineAst.PipelineElements.Count != 0)
            {
                throw new InvalidPowerShellExpressionException("Cannot use pipelines in expressions");
            }

            if (!(pipelineAst.PipelineElements[0] is CommandExpressionAst commandExpressionAst))
            {
                throw new InvalidPowerShellExpressionException($"Pipeline element '{pipelineAst.PipelineElements[0]}' is not a command expression");
            }

            return commandExpressionAst.Expression;
        }
    }
}
