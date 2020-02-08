namespace Microsoft.Windows.PowerShell.ScriptAnalyzer.BuiltinRules
{
    public class UseDeclaredVarsMoreThanAssignmentsPester
    {
        private class Visitor : AstVisitor
        {

            public override AstVisitAction VisitCommand(CommandAst commandAst)
            {

                string commandName = commandAst.GetCommandName();
                if (commandName != null
                    && TryGetPesterBlockType(commandName, out PesterBlockType blockType))
                {
                    IEnumerable<ScriptBlockAst> scriptBlockArguments = GetScriptBlockAstsFromCommandElements(commandAst.CommandElements);
                    if (scriptBlockArguments.Count() == 1)
                    {
                        _currentPesterBlock = new PesterBlock(
                            blockType,
                            body: scriptBlockArguments.First(),
                            parent: _currentPesterBlock);

                        _currentPesterBlock.Parent.SubBlocks.Add(_currentPesterBlock);
                    }
                    return AstVisitAction.Continue;
                }

                if (_currentPesterBlock != null)
                {
                    return AstVisitAction.Continue;
                }
            }

            private bool TryGetPesterBlockType(string commandName, out PesterBlockType type)
            {
                if (String.Equals(commandName, "It", StringComparison.OrdinalIgnoreCase))
                {
                    type = PesterBlockType.It;
                    return true;
                }

                if (String.Equals(commandName, "Context", StringComparison.OrdinalIgnoreCase))
                {
                    type = PesterBlockType.Context;
                    return true;
                }

                if (String.Equals(commandName, "Describe", StringComparison.OrdinalIgnoreCase))
                {
                    type = PesterBlockType.Describe;
                    return true;
                }

                if (String.Equals(commandName, "BeforeAll", StringComparison.OrdinalIgnoreCase))
                {
                    type = PesterBlockType.BeforeAll;
                    return true;
                }

                if (String.Equals(commandName, "BeforeEach", StringComparison.OrdinalIgnoreCase))
                {
                    type = PesterBlockType.BeforeEach;
                    return true;
                }

                type = PesterBlockType.None;
                return false;
            }

            private void RunPesterAnalysis()
            {
                void RunPesterAnalysisHelper(
                    PesterBlock currentBlock,
                    Stack<Dictionary<string, ExpressionAst>> variables)
                {
                    variables.Push(new Dictionary<string, ExpressionAst>(StringComparer.OrdinalIgnoreCase));

                    var visitor = new PesterVisitor(variables);

                    // Process Before blocks
                    foreach (PesterBlock block in currentBlock.SubBlocks)
                    {
                        if (block.Type == PesterBlockType.BeforeAll
                            || block.Type == PesterBlockType.BeforeEach)
                        {
                            block.Body.Visit(visitor);
                        }
                    }

                    // Now 
                }

                RunPesterAnalysisHelper(_currentPesterBlock, new Stack<Dictionary<string, ExpressionAst>>());
            }
        }

        private enum PesterBlockType
        {
            None = 0,
            Describe,
            Context,
            It,
            BeforeAll,
            BeforeEach
        }

        private class PesterBlock
        {
            public PesterBlock(
                PesterBlockType type,
                ScriptBlockAst body,
                PesterBlock parent)
            {
                Type = type;
                Body = body;
                Parent = parent;
                SubBlocks = new List<PesterBlock>();
            }

            public PesterBlockType Type { get; }

            public ScriptBlockAst Body { get; }

            public List<PesterBlock> SubBlocks { get; }

            public PesterBlock Parent { get; }
        }

        private class PesterVisitor : AstVisitor
        {
            private readonly Stack<Dictionary<string, ExpressionAst>> _unusedVariableDefinitionStack;

            public PesterVisitor(
                Stack<Dictionary<string, ExpressionAst>> unusedVariableDefinitionStack)
            {
                _unusedVariableDefinitionStack = unusedVariableDefinitionStack;
            }

            public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
            {
                assignmentStatementAst.Right.Visit(this);
            }
        }
        }
    }
}