using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class ScriptAnalyzerBuilder
    {
        private readonly List<Assembly> _ruleAssemblies;

        private readonly List<string> _ruleAssemblyPaths;

        private readonly string _configurationPath;

        private readonly HashtableAst _configurationAst;

        public ScriptAnalyzerBuilder()
        {
        }

        public ScriptAnalyzerBuilder AddRuleAssembly(string assemblyPath)
        {
            return this;
        }

        public ScriptAnalyzerBuilder AddRuleAssembly(Assembly assembly)
        {
            return this;
        }

        public ScriptAnalyzer Build()
        {
            return null;
        }
    }
}
