using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.ScriptAnalyzer.Tools
{
    public static class AstExtensions
    {
        public static IScriptExtent GetFunctionNameExtent(this FunctionDefinitionAst functionDefinitionAst, IReadOnlyList<Token> tokens)
        {
            foreach (Token token in tokens)
            {
                if (functionDefinitionAst.Extent.Contains(token.Extent)
                    && token.Text.Equals(functionDefinitionAst.Name))
                {
                    return token.Extent;
                }
            }

            return null;
        }

        public static object GetValue(this NamedAttributeArgumentAst namedAttributeArgumentAst)
        {
            if (namedAttributeArgumentAst.ExpressionOmitted)
            {
                return true;
            }

            return AstTools.GetSafeValueFromAst(namedAttributeArgumentAst.Argument);
        }
    }
}
