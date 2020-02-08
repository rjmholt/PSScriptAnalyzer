// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
#if !CORECLR
using System.ComponentModel.Composition;
#endif
using System.Globalization;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Microsoft.Windows.PowerShell.ScriptAnalyzer.BuiltinRules
{
    /// <summary>
    /// UseDeclaredVarsMoreThanAssignments: Analyzes the ast to check that variables are used in more than just their assignment.
    /// </summary>
#if !CORECLR
[Export(typeof(IScriptRule))]
#endif
    public class UseDeclaredVarsMoreThanAssignments : IScriptRule
    {
        /// <summary>
        /// AnalyzeScript: Analyzes the ast to check that variables are used in more than just there assignment.
        /// </summary>
        /// <param name="ast">The script's ast</param>
        /// <param name="fileName">The script's file name</param>
        /// <returns>A List of results from this rule</returns>
        public IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string fileName)
        {
            if (ast == null)
            {
                throw new ArgumentNullException(Strings.NullAstErrorMessage);
            }

            var scriptBlockAsts = ast.FindAll(x => x is ScriptBlockAst, true);
            if (scriptBlockAsts == null)
            {
                yield break;
            }

            foreach (var scriptBlockAst in scriptBlockAsts)
            {
                var sbAst = scriptBlockAst as ScriptBlockAst;
                foreach (var diagnosticRecord in AnalyzeScriptBlockAst(sbAst, fileName))
                {
                    yield return diagnosticRecord;
                }
            }
        }

        /// <summary>
        /// GetName: Retrieves the name of this rule.
        /// </summary>
        /// <returns>The name of this rule</returns>
        public string GetName()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.NameSpaceFormat, GetSourceName(), Strings.UseDeclaredVarsMoreThanAssignmentsName);
        }

        /// <summary>
        /// GetCommonName: Retrieves the common name of this rule.
        /// </summary>
        /// <returns>The common name of this rule</returns>
        public string GetCommonName()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.UseDeclaredVarsMoreThanAssignmentsCommonName);
        }

        /// <summary>
        /// GetDescription: Retrieves the description of this rule.
        /// </summary>
        /// <returns>The description of this rule</returns>
        public string GetDescription()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.UseDeclaredVarsMoreThanAssignmentsDescription);
        }

        /// <summary>
        /// GetSourceType: Retrieves the type of the rule: builtin, managed or module.
        /// </summary>
        public SourceType GetSourceType()
        {
            return SourceType.Builtin;
        }

        /// <summary>
        /// GetSeverity: Retrieves the severity of the rule: error, warning of information.
        /// </summary>
        /// <returns></returns>
        public RuleSeverity GetSeverity()
        {
            return RuleSeverity.Warning;
        }

        /// <summary>
        /// GetSourceName: Retrieves the module/assembly name the rule is from.
        /// </summary>
        public string GetSourceName()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.SourceName);
        }

        /// <summary>
        /// Checks if a variable is initialized and referenced in either its assignment or children scopes
        /// </summary>
        /// <param name="scriptBlockAst">Ast of type ScriptBlock</param>
        /// <param name="fileName">Name of file containing the ast</param>
        /// <returns>An enumerable containing diagnostic records</returns>
        private IEnumerable<DiagnosticRecord> AnalyzeScriptBlockAst(ScriptBlockAst scriptBlockAst, string fileName)
        {
            var visitor = new Visitor(this, fileName);
            scriptBlockAst.Visit(visitor);
            return visitor.GetDiagnostics();
        }

        private class Visitor : AstVisitor, IAstPostVisitHandler
        {
            // List of known dot-sourcing commands. Boolean is meaningless - allows concurrent hash lookup
            private static ConcurrentDictionary<string, bool> s_dotSourcingCommands = new ConcurrentDictionary<string, bool>(new [] {
                new KeyValuePair<string, bool>("ForEach-Object", true),
                new KeyValuePair<string, bool>("%", true ),
                new KeyValuePair<string, bool>("Where-Object", true),
                new KeyValuePair<string, bool>("?", true),
            }, StringComparer.OrdinalIgnoreCase);

            private static ConcurrentDictionary<string, bool> s_variableCreationCommands = new ConcurrentDictionary<string, bool>(new [] {
                new KeyValuePair<string, bool>("New-Variable", true),
                new KeyValuePair<string, bool>("nv", true),
                new KeyValuePair<string, bool>("Set-Variable", true),
                new KeyValuePair<string, bool>("sv", true),
            }, StringComparer.OrdinalIgnoreCase);

            private static ConcurrentDictionary<string, string> s_variableCreationCommandSwitches = new ConcurrentDictionary<string, string>(new [] {
                new KeyValuePair<string, string>("Force", "f"),
                new KeyValuePair<string, string>("PassThru", "p"),
                new KeyValuePair<string, string>("WhatIf", "wh"),
                new KeyValuePair<string, string>("Confirm", "c"),
            });

            private static ConcurrentDictionary<string, string> s_getVariableCommandSwitches = new ConcurrentDictionary<string, string>(new []
            {
                new KeyValuePair<string, string>("ValueOnly", "v"),
            });

            private readonly IRule _rule;
            private readonly string _scriptPath;
            private readonly List<DiagnosticRecord> _diagnostics;
            private readonly Stack<KeyValuePair<ScriptBlockAst, Dictionary<string, Ast>>> _scriptBlockContext;
            private readonly Dictionary<string, Ast> _scriptScopeVariables;
            private readonly HashSet<ScriptBlockAst> _dotSourcedScriptBlocks;

            public Visitor(IRule rule, string scriptPath)
            {
                _rule = rule;
                _scriptPath = scriptPath;
                _diagnostics = new List<DiagnosticRecord>();
                _scriptScopeVariables = new Dictionary<string, Ast>(StringComparer.OrdinalIgnoreCase);
                _scriptBlockContext = new Stack<KeyValuePair<ScriptBlockAst, Dictionary<string, Ast>>>();
                _dotSourcedScriptBlocks = new HashSet<ScriptBlockAst>();
            }

            public IEnumerable<DiagnosticRecord> GetDiagnostics()
            {
                return _diagnostics;
            }

            public void PostVisit(Ast ast)
            {
                // If we have no AST context on the stack, there's nothing to do
                if (_scriptBlockContext.Count == 0)
                {
                    return;
                }

                // See if we're leaving the context of the last scriptblock we entered
                // and if so, pop it off and see if any of the variables in that scope went unused
                //
                // NOTE: We don't look up the stack for variables,
                // since PowerShell has dynamic, and not lexical, scope;
                // looking up the stack only happens at runtime, so it's something we can't analyze
                if (_scriptBlockContext.Peek().Key == ast)
                {
                    Dictionary<string, Ast> unusedVariables = _scriptBlockContext.Pop().Value;
                    foreach (ExpressionAst variableDefinition in unusedVariables.Values)
                    {
                        if (!TryGetVariableNameFromExpression(variableDefinition, out string variableName))
                        {
                            // We should only have added variable asts and set/new-variable arguments
                            throw new InvalidOperationException(
                                $"Unexpected variable AST recorded '{variableDefinition}' of type '{variableDefinition.GetType().FullName}'");
                        }

                        _diagnostics.Add(
                            new DiagnosticRecord(
                                string.Format(CultureInfo.CurrentCulture, Strings.UseDeclaredVarsMoreThanAssignmentsError, variableName),
                                variableDefinition.Extent,
                                _rule.GetName(),
                                DiagnosticSeverity.Warning,
                                _scriptPath));
                    }
                }
            }

            public override AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst)
            {
                // If we're not looking at a scriptblock that's being dot-sourced, push a new scope
                if (!_dotSourcedScriptBlocks.Remove(scriptBlockAst))
                {
                    _scriptBlockContext.Push(
                        new KeyValuePair<ScriptBlockAst, Dictionary<string, Ast>>(
                            scriptBlockAst,
                            new Dictionary<string, Ast>(StringComparer.OrdinalIgnoreCase)));
                }

                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
            {
                Dictionary<string, Ast> scopeVariables = _scriptBlockContext.Peek().Value;

                // Want to visit the RHS to check for used variables
                // We visit it first since it's evaluated first, so we catch '$x = $x' when $x has never been set
                assignmentStatementAst.Right.Visit(this);

                switch (assignmentStatementAst.Left)
                {
                    case MemberExpressionAst memberExpressionAst:
                        memberExpressionAst.Visit(this);
                        break;

                    case ArrayLiteralAst arrayLhs:
                        foreach (ExpressionAst expression in arrayLhs.Elements)
                        {
                            VariableExpressionAst arrayVariableAst = GetVariableAstFromExpression(expression);
                            scopeVariables[arrayVariableAst.VariablePath.UserPath] = arrayVariableAst;
                        }

                        break;

                    default:
                        VariableExpressionAst variableExpressionAst = GetVariableAstFromExpression(assignmentStatementAst.Left);
                        scopeVariables[variableExpressionAst.VariablePath.UserPath] = variableExpressionAst;
                        break;
                }

                // We don't want to visit the LHS,
                // since we don't want to register them as variable usages
                return AstVisitAction.SkipChildren;
            }

            public override AstVisitAction VisitCommand(CommandAst commandAst)
            {
                // If the command is mysteriously absent, move on with our lives
                if (commandAst.CommandElements == null || commandAst.CommandElements.Count == 0)
                {
                    return AstVisitAction.Continue;
                }

                // Dot sourcing brings a script block into the current scope
                if (commandAst.InvocationOperator == TokenKind.Dot)
                {
                    switch (commandAst.CommandElements[0])
                    {
                        case ScriptBlockExpressionAst scriptBlockExpression:
                            _dotSourcedScriptBlocks.Add(scriptBlockExpression.ScriptBlock);
                            break;
                    }

                    return AstVisitAction.Continue;
                }

                string commandName = commandAst.GetCommandName();
                if (commandName == null)
                {
                    return AstVisitAction.Continue;
                }

                // If the next command effectively dot-sources a scriptblock,
                // mark this and continue
                if (s_dotSourcingCommands.ContainsKey(commandName))
                {
                    foreach (ScriptBlockAst scriptBlock in GetScriptBlockAstsFromCommandElements(commandAst.CommandElements))
                    {
                        _dotSourcedScriptBlocks.Add(scriptBlock);
                    }
                    return AstVisitAction.Continue;
                }

                Dictionary<string, Ast> scopeVariables = _scriptBlockContext.Peek().Value;

                // We may encounter a Set-Variable (etc), which we treat as assignment
                // The parameters here happen to be common to the variable definition cmdlets
                // If s_variableCreateCommands is updated, this logic will need to be altered
                if (s_variableCreationCommands.ContainsKey(commandName)
                    && TryGetVariableNameFromParameters(commandAst.CommandElements, s_variableCreationCommandSwitches, out ExpressionAst createdVariableNameExpression))
                {
                    if (createdVariableNameExpression is StringConstantExpressionAst stringConstantExpression)
                    {
                        scopeVariables[stringConstantExpression.Value] = createdVariableNameExpression;
                    }

                    return AstVisitAction.Continue;
                }

                // Get-Variable behaves as a variable reference
                if ((String.Equals(commandName, "Get-Variable", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(commandName, "gv", StringComparison.OrdinalIgnoreCase))
                    && TryGetVariableNameFromParameters(commandAst.CommandElements, s_getVariableCommandSwitches, out ExpressionAst usedVariableExpression))
                {
                    if (usedVariableExpression is StringConstantExpressionAst stringConstantExpression)
                    {
                        scopeVariables.Remove(stringConstantExpression.Value);
                    }

                    return AstVisitAction.Continue;
                }

                return AstVisitAction.Continue;
            }

            public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
            {
                // Remove this variable from the table of defined variables if we see it
                _scriptBlockContext.Peek().Value.Remove(variableExpressionAst.VariablePath.UserPath);

                return AstVisitAction.SkipChildren;
            }

            public IEnumerable<ScriptBlockAst> GetScriptBlockAstsFromCommandElements(
                ReadOnlyCollection<CommandElementAst> commandElements)
            {
                foreach (CommandElementAst commandElement in commandElements)
                {
                    switch (commandElement)
                    {
                        case ScriptBlockExpressionAst scriptBlockExpression:
                            yield return scriptBlockExpression.ScriptBlock;
                            break;
                    }
                }
            }

            private void RegisterVariableDeclaration(string variableName, Ast definingAst, VariableScope scope)
            {
                switch (scope)
                {
                    case VariableScope.Private:
                    case VariableScope.Local:
                    case VariableScope.Normal:
                        _scriptBlockContext.Peek().Value[variableName] = definingAst;
                        return;

                    case VariableScope.Script:
                        _scriptScopeVariables[variableName] = definingAst;
                        return;
                }
            }

            private void RegisterVariableUse(string variableName, VariableScope scope)
            {
                switch (scope)
                {
                    case VariableScope.Private:
                    case VariableScope.Local:
                    case VariableScope.Normal:
                        _scriptBlockContext.Peek().Value.Remove(variableName);
                        return;

                    case VariableScope.Script:
                        _scriptScopeVariables.Remove(variableName);
                        return;
                }
            }

            private static bool TryGetVariableNameFromParameters(
                ReadOnlyCollection<CommandElementAst> commandElements,
                IReadOnlyDictionary<string, string> switchParameters,
                out ExpressionAst parameterValueExpression)
            {
                // We have three possibilities with multiple cases:
                // - The value is passed positionally:
                //   + Set-Variable x 'Hi'
                //   + Set-Variable -Value 'Hi' x
                //   + Set-Variable -Value 'Hi' -Force x
                // - The value is passed by parameter:
                //   + Set-Variable -Name x 'Hi'
                //   + Set-Variable -Name:x 'Hi'
                //   + Set-Variable 'Hi' -Name x
                //   + Set-Variable -Force 'Hi' -Name x
                // - The command is semantically invalid (in which case we ignore it):
                //   + Set-Variable -Name -Force x 'Hi'
                //   + Set-Variable -Name x -Value
                //
                // We're forced to collect all the parameters because of cases like:
                //   Set-Variable 'Hi' -Name x

                bool seenFirstPosition = false;
                string currentParameterName = null;
                ExpressionAst firstPositionalParameter = null;
                var namedParameters = new Dictionary<string, ExpressionAst>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < commandElements.Count; i++)
                {
                    CommandElementAst commandElement = commandElements[i];

                    switch (commandElement)
                    {
                        case CommandParameterAst parameterAst:
                            // The command is invalid
                            if (currentParameterName != null)
                            {
                                parameterValueExpression = null;
                                return false;
                            }

                            // Skip over switches
                            if (IsInParameterDict(switchParameters, parameterAst.ParameterName))
                            {
                                continue;
                            }

                            // Collect parameters that come with their argument
                            if (parameterAst.Argument != null)
                            {
                                namedParameters[parameterAst.ParameterName] = parameterAst.Argument;
                                continue;
                            }

                            // Set up collecting the argument for this parameter
                            currentParameterName = parameterAst.ParameterName;
                            continue;

                        case ExpressionAst argumentAst:
                            // Collect the argument if we have the name
                            if (currentParameterName != null)
                            {
                                namedParameters[currentParameterName] = argumentAst;
                                currentParameterName = null;
                                continue;
                            }

                            // If this is the first positional parameter, remember it
                            if (!seenFirstPosition)
                            {
                                firstPositionalParameter = argumentAst;
                                seenFirstPosition = true;
                            }
                            continue;
                    }
                }

                if (namedParameters.TryGetValue("Name", out parameterValueExpression)
                    || namedParameters.TryGetValue("n", out parameterValueExpression)
                    || namedParameters.TryGetValue("na", out parameterValueExpression)
                    || namedParameters.TryGetValue("nam", out parameterValueExpression))
                {
                    return true;
                }

                parameterValueExpression = firstPositionalParameter;
                return parameterValueExpression != null;

            }

            private static VariableExpressionAst GetVariableAstFromExpression(ExpressionAst expressionAst)
            {
                switch (expressionAst)
                {
                    case VariableExpressionAst variableExpressionAst:
                        return variableExpressionAst;

                    case AttributedExpressionAst attributedExpressionAst:
                        return GetVariableAstFromExpression(attributedExpressionAst.Child);

                    default:
                        throw new ArgumentException($"Assignment LHS '{expressionAst.Extent.Text}' was of unexpected type: '{expressionAst.GetType().FullName}'");
                }
            }

            private static bool TryGetVariableNameFromExpression(ExpressionAst expressionAst, out string variableName)
            {
                switch (expressionAst)
                {
                    case VariableExpressionAst variableExpressionAst:
                        variableName = variableExpressionAst.VariablePath.UserPath;
                        return true;

                    case StringConstantExpressionAst stringConstantExpressionAst:
                        variableName = stringConstantExpressionAst.Value;
                        return true;

                    default:
                        variableName = null;
                        return false;
                }
            }

            private static bool IsInParameterDict(IReadOnlyDictionary<string, string> parameters, string parameterName)
            {
                if (parameters.ContainsKey(parameterName))
                {
                    return true;
                }

                foreach (KeyValuePair<string, string> possibleParameter in parameters)
                {
                    if (parameterName.StartsWith(possibleParameter.Value)
                        && possibleParameter.Key.IndexOf(parameterName) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private enum VariableScope
        {
            Unknown = 0,
            Normal,
            Private,
            Local,
            Script,
            Global,
            Env,
            Using,
        }
    }
}
