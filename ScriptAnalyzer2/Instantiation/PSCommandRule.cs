using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    internal class PSCommandRule : ScriptRule
    {
        private static readonly Func<CommandInfo, Command> s_createCommandFromCommandInfo;

        static PSCommandRule()
        {
            ParameterExpression commandInfoParameter = Expression.Parameter(typeof(CommandInfo));
            s_createCommandFromCommandInfo = Expression.Lambda<Func<CommandInfo, Command>>(
                Expression.New(
                    typeof(Command).GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        binder: null,
                        types: new[] { typeof(CommandInfo) },
                        modifiers: null), commandInfoParameter), commandInfoParameter).Compile();
        }

        private readonly CommandInfo _command;

        private readonly IPowerShellRuleExecutor _executor;

        public PSCommandRule(
            RuleInfo ruleInfo,
            IPowerShellRuleExecutor executor,
            CommandInfo commandInfo)
            : base(ruleInfo)
        {
            _executor = executor;
            _command = commandInfo;
        }

        public override IEnumerable<ScriptDiagnostic> AnalyzeScript(Ast ast, IReadOnlyList<Token> tokens, string scriptPath)
        {
            var command = new PSCommand()
                .AddCommand(s_createCommandFromCommandInfo(_command))
                .AddParameter("Ast", ast)
                .AddParameter("Tokens", tokens)
                .AddParameter("ScriptPath", scriptPath);

            foreach (PSObject output in _executor.InvokeCommand<PSObject>(command))
            {
                if (output?.BaseObject is ScriptDiagnostic diagnostic)
                {
                    yield return diagnostic;
                }
            }
        }
    }
}
