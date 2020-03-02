using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Text;

namespace Microsoft.Windows.PowerShell.ScriptAnalyzer
{
    internal class ParameterBindingResult
    {
        public IReadOnlyList<ExpressionAst> PositionalParameters { get; }

        public IReadOnlyDictionary<string, KeyValuePair<CommandParameterAst, ExpressionAst>> NamedParameters { get; }
    }

    internal interface IParameterBindingSimulator
    {

    }
}
