using Frisia.Solver;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SK = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Frisia.Rewriter
{
    public sealed class FrisiaSyntaxRewriter : CSharpSyntaxRewriter
    {
        private readonly ILogger logger;
        private readonly ISolver solver;
        private FrisiaSyntaxRewriter rewriterTrue = null;
        private FrisiaSyntaxRewriter rewriterFalse = null;
        private List<string[]> results;

        private FrisiaSyntaxRewriter RewriterTrue
        {
            get
            {
                return rewriterTrue ??
                    (rewriterTrue = new FrisiaSyntaxRewriter(ConditionsTrue, Parameters, SMS, solver, logger, LoopIterations, VisitUnsatisfiablePaths));
            }
        }
        private FrisiaSyntaxRewriter RewriterFalse
        {
            get
            {
                return rewriterFalse ??
                    (rewriterFalse = new FrisiaSyntaxRewriter(ConditionsFalse, Parameters, SMS, solver, logger, LoopIterations, VisitUnsatisfiablePaths));
            }
        }

        public IList<ExpressionSyntax> ConditionsTrue { get; private set; }
        public IList<ExpressionSyntax> ConditionsFalse { get; private set; }
        public SeparatedSyntaxList<ParameterSyntax> Parameters { get; private set; }
        public SymbolicMemoryState SMS { get; private set; }

        public bool VisitUnsatisfiablePaths { get; private set; }
        public uint LoopIterations { get; private set; }

        public FrisiaSyntaxRewriter(
            IList<ExpressionSyntax> conditions,
            SeparatedSyntaxList<ParameterSyntax> parameters,
            SymbolicMemoryState sms,
            ISolver solver,
            ILogger logger,
            uint loopIterations = 1,
            bool visitUnsatisfiablePaths = false)
        {
            this.logger = logger;
            this.solver = solver;
            LoopIterations = loopIterations > 0 ? loopIterations : 1;
            ConditionsTrue = new List<ExpressionSyntax>(conditions);
            ConditionsFalse = new List<ExpressionSyntax>(conditions);
            Parameters = parameters;
            SMS = new SymbolicMemoryState(sms);
            results = new List<string[]>();
            VisitUnsatisfiablePaths = visitUnsatisfiablePaths;
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
        {
            var logPathTrue = "";
            var logPathFalse = "";
            BlockSyntax childBlock;
            StatementSyntax statementTrue = null;
            StatementSyntax statementFalse = null;

            // Separate multi-component conditions
            node = SeparateIfConditions(node);

            var condition = VisitExpression(node.Condition);

            // If - true path
            ConditionsTrue.Add(condition);
            foreach (var c in ConditionsTrue)
            {
                if (c.IsKind(SK.LogicalNotExpression))
                {
                    logPathTrue += $"{c} && ";
                }
                else
                {
                    logPathTrue += $"({c}) && ";
                }
            }
            var modelTrue = solver.GetModel(Parameters, ConditionsTrue);

            childBlock = SF.Block(GetElementsFromBlock(node.ChildNodes().OfType<StatementSyntax>()));

            if (modelTrue != null)
            {
                // Check if end of path
                var ifTrueChild = GetStatementAsBlock(node.Statement).ChildNodes();
                if (ifTrueChild.OfType<ReturnStatementSyntax>().ToArray().Length != 0 ||
                    ifTrueChild.OfType<ThrowStatementSyntax>().ToArray().Length != 0)
                {
                    results.Add(modelTrue);
                    logger.Info("TRUE PATH: " + logPathTrue.TrimEnd(' ', '&'));
                }

                statementTrue = (StatementSyntax)RewriterTrue.Visit(childBlock);
            }
            else
            {
                logger.Trace("UNSATISFIABLE: " + logPathTrue.TrimEnd(' ', '&'));

                if (VisitUnsatisfiablePaths)
                {
                    statementTrue = (StatementSyntax)RewriterTrue.Visit(childBlock);
                }
                else
                {
                    // Do not visit unsatisfiable path
                    statementTrue = childBlock;
                }

            }

            // Else - false path
            var negatedCondition = SF.PrefixUnaryExpression(SK.LogicalNotExpression, SF.ParenthesizedExpression(condition));
            ConditionsFalse.Add(negatedCondition);
            foreach (var c in ConditionsFalse)
            {
                if (c.IsKind(SK.LogicalNotExpression))
                {
                    logPathFalse += $"{c} && ";
                }
                else
                {
                    logPathFalse += $"({c}) && ";
                }
            }
            var modelFalse = solver.GetModel(Parameters, ConditionsFalse);

            if (modelFalse != null)
            {
                if (node.Else != null)
                {
                    // Check if end of path
                    var ifFalseChild = GetStatementAsBlock(node.Else.Statement).ChildNodes();
                    if (ifFalseChild.OfType<ReturnStatementSyntax>().ToArray().Length != 0 ||
                        ifFalseChild.OfType<ThrowStatementSyntax>().ToArray().Length != 0)
                    {
                        results.Add(modelFalse);
                        logger.Info("FALSE PATH: " + logPathFalse.TrimEnd(' ', '&'));
                    }

                    childBlock = SF.Block(GetElementsFromBlock(node.Else.ChildNodes().OfType<StatementSyntax>()));
                    statementFalse = (StatementSyntax)RewriterFalse.Visit(childBlock);
                }
                else
                {
                    // End of path and nothing to return
                    return node;
                }
            }
            else
            {
                logger.Trace("UNSATISFIABLE: " + logPathFalse.TrimEnd(' ', '&'));

                if (node.Else != null)
                {
                    childBlock = SF.Block(GetElementsFromBlock(node.Else.ChildNodes().OfType<StatementSyntax>()));
                    if (VisitUnsatisfiablePaths)
                    {
                        statementFalse = (StatementSyntax)RewriterFalse.Visit(childBlock);
                    }
                    else
                    {
                        // Do not visit unsatisfiable path
                        statementFalse = childBlock;
                    }
                }
                else
                {
                    // End of path and nothing to return
                    return node;
                }
            }
            return SF.IfStatement(node.Condition, statementTrue, SF.ElseClause(statementFalse));
        }

        private IfStatementSyntax SeparateIfConditions(IfStatementSyntax node)
        {
            if (node.Condition.IsKind(SK.InvocationExpression))
            {
                return node;
            }
            else
            {
                var conditionChilds = node.Condition.ChildNodes().ToArray();
                if (conditionChilds.Length == 2)
                {

                    IfStatementSyntax aStatement, bStatement;
                    ExpressionSyntax aCondition, bCondition = null;
                    BlockSyntax elseBlock = null;
                    ElseClauseSyntax elseClause = null;

                    aCondition = (ExpressionSyntax)conditionChilds[0];
                    bCondition = (ExpressionSyntax)conditionChilds[1];

                    var ifBlock = GetStatementAsBlock(node.ChildNodes().OfType<StatementSyntax>().First());
                    if (node.Else != null)
                    {
                        elseBlock = GetStatementAsBlock(node.Else.ChildNodes().OfType<StatementSyntax>().First());
                        elseClause = elseBlock != null ? SF.ElseClause(elseBlock) : null;
                    }

                    switch (node.Condition.Kind())
                    {
                        case SK.LogicalAndExpression:
                            bStatement = SF.IfStatement(bCondition, ifBlock, elseClause);
                            aStatement = SF.IfStatement(aCondition, SF.Block(bStatement), elseClause);
                            node = aStatement;
                            break;
                        case SK.LogicalOrExpression:
                            bStatement = SF.IfStatement(bCondition, ifBlock, elseClause);
                            aStatement = SF.IfStatement(aCondition, ifBlock, SF.ElseClause(SF.Block(bStatement)));
                            node = aStatement;
                            break;
                        default:
                            return SF.IfStatement(node.Condition, ifBlock, elseClause);
                    }
                }

                if (node.Condition.ChildNodes().ToArray().Length == 2)
                {
                    return SeparateIfConditions(node);
                }
                return node;
            }

        }

        private BlockSyntax GetStatementAsBlock(StatementSyntax statement)
        {
            BlockSyntax block;
            if (statement.IsKind(SK.Block))
            {
                block = (BlockSyntax)statement;
            }
            else
            {
                block = SF.Block(statement);
            }
            return block;
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            //ConditionsTrue = ConditionsFalse = null;
            return base.VisitReturnStatement(node);
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            // Step I: rewrite loops
            var children = node.ChildNodes().OfType<StatementSyntax>().ToArray();

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].IsKind(SK.WhileStatement))
                {
                    children[i] = (StatementSyntax)VisitWhileStatement(children[i] as WhileStatementSyntax);
                }
                if (children[i].IsKind(SK.ForStatement))
                {
                    children[i] = (StatementSyntax)VisitForStatement(children[i] as ForStatementSyntax);
                }
            }

            // Step II: join child blocks - {{...}{...}{...}}
            var childrenList = new List<StatementSyntax>();

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].IsKind(SK.Block))
                {
                    childrenList.AddRange(children[i].ChildNodes().OfType<StatementSyntax>());
                }
                else
                {
                    childrenList.Add(children[i]);
                }
            }
            children = childrenList.ToArray();

            // Step III: rewrite if statements to if-else
            var index = GetIndexOfOneBeforeLastStatement(children);
            while (index != -1)
            {
                StatementSyntax newStatement = null;
                var oneBeforeLastChild = children[index];
                // TakeLast(children.Length - 1 - index);
                var remainingChildren = children.Skip(Math.Max(0, children.Length - (children.Length - 1 - index)));

                var oneBeforeLastChildStatements = GetElementsFromBlock(oneBeforeLastChild.ChildNodes().OfType<StatementSyntax>());
                IEnumerable<StatementSyntax> statements;

                if (oneBeforeLastChildStatements.Count() > 0 &&
                    (oneBeforeLastChildStatements.Last().IsKind(SK.ThrowStatement) ||
                    oneBeforeLastChildStatements.Last().IsKind(SK.ReturnStatement)))
                {
                    // If last statement is a throw or return, do not append last statement
                    statements = oneBeforeLastChildStatements;
                }
                else
                {
                    var statementsList = oneBeforeLastChildStatements.ToList();
                    statementsList.AddRange(remainingChildren);
                    statements = statementsList;
                }

                if (oneBeforeLastChild.IsKind(SK.IfStatement))
                {
                    var ifStatement = (IfStatementSyntax)oneBeforeLastChild;
                    var elseChilds = GetElementsFromBlock(ifStatement.Else?.ChildNodes().OfType<StatementSyntax>());
                    IEnumerable<StatementSyntax> elseStatements;
                    if (elseChilds != null)
                    {
                        if (elseChilds.Count() > 0 &&
                            (elseChilds.Last().IsKind(SK.ThrowStatement) ||
                            elseChilds.Last().IsKind(SK.ReturnStatement)))
                        {
                            elseStatements = elseChilds;
                        }
                        else
                        {
                            var elseStatementsList = elseChilds.ToList();
                            elseStatementsList.AddRange(remainingChildren);
                            elseStatements = elseStatementsList;
                        }
                    }
                    else
                    {
                        elseStatements = new List<StatementSyntax>(remainingChildren);
                    }
                    var mainBlock = SF.Block(statements);
                    var elseBlock = SF.Block(elseStatements);

                    ElseClauseSyntax elseClause = elseStatements.Count() > 0 ? SF.ElseClause(elseBlock) : null;

                    newStatement = SF.IfStatement(ifStatement.Condition, mainBlock, elseClause);
                }
                else
                {
                    throw new NotImplementedException(oneBeforeLastChild.Kind().ToString());
                }

                if (newStatement != null)
                {
                    children = children.Take(index).Append(newStatement).ToArray();
                }
                else
                {
                    children = children.Take(index + 1).ToArray();
                }

                index = GetIndexOfOneBeforeLastStatement(children);
            }

            var block = SF.Block(children);
            var result = base.VisitBlock(block);
            return result;
        }

        public IEnumerable<string[]> GetResults()
        {
            var totalResults = new List<string[]>(results);
            if (rewriterTrue != null)
            {
                totalResults.AddRange(rewriterTrue.GetResults());
            }
            if (rewriterFalse != null)
            {
                totalResults.AddRange(rewriterFalse.GetResults());
            }
            return totalResults;
        }

        private int GetIndexOfOneBeforeLastStatement(StatementSyntax[] children)
        {
            var count = 0;
            for (int i = children.Length - 1; i >= 0; i--)
            {
                if (!children[i].IsKind(SK.LocalDeclarationStatement) &&
                    !children[i].IsKind(SK.IfStatement) &&
                    !children[i].IsKind(SK.ReturnStatement) &&
                    !children[i].IsKind(SK.ThrowStatement) &&
                    !children[i].IsKind(SK.ForStatement) &&
                    !children[i].IsKind(SK.WhileStatement) &&
                    !children[i].IsKind(SK.ExpressionStatement) &&
                    !children[i].IsKind(SK.Block))
                {
                    throw new NotImplementedException(children[i].Kind().ToString());
                }
                if (!children[i].IsKind(SK.ExpressionStatement) &&
                    !children[i].IsKind(SK.LocalDeclarationStatement))
                {
                    if (++count == 2)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private IEnumerable<StatementSyntax> GetElementsFromBlock(IEnumerable<StatementSyntax> elementsOrBlock)
        {
            if (elementsOrBlock != null && elementsOrBlock.Count() == 1 && elementsOrBlock.Single().IsKind(SK.Block))
            {
                return elementsOrBlock.Single().ChildNodes().OfType<StatementSyntax>();
            }
            return elementsOrBlock;
        }

        public ExpressionSyntax VisitExpression(ExpressionSyntax node)
        {
            if (node is BinaryExpressionSyntax binaryExpression)
            {
                var left = VisitExpression(binaryExpression.Left);
                var right = VisitExpression(binaryExpression.Right);
                return SF.BinaryExpression(node.Kind(), left, right);
            }
            if (node is IdentifierNameSyntax identifierName)
            {
                return SMS.Variables[identifierName.Identifier.Text];
            }
            if (node is CastExpressionSyntax castExpression)
            {
                return SF.CastExpression(castExpression.Type, VisitExpression(castExpression.Expression));
            }
            if (node is InvocationExpressionSyntax invocationExpression)
            {
                var arguments = SF.ArgumentList();
                foreach (var a in invocationExpression.ArgumentList.Arguments)
                {
                    arguments = arguments.AddArguments(VisitArgument(a));
                }
                return SF.InvocationExpression(invocationExpression.Expression, arguments);
            }
            if (node is PrefixUnaryExpressionSyntax prefixUnaryExpression)
            {
                return SF.PrefixUnaryExpression(node.Kind(), VisitExpression(prefixUnaryExpression.Operand));
            }
            if (node is ParenthesizedExpressionSyntax parenthesizedExpressionSyntax)
            {
                return SF.ParenthesizedExpression(VisitExpression(parenthesizedExpressionSyntax.Expression));
            }
            if (node is LiteralExpressionSyntax ||
                node is MemberAccessExpressionSyntax)
            {
                return node;
            }
            throw new NotImplementedException(node.GetType().Name);
        }

        public new ArgumentSyntax VisitArgument(ArgumentSyntax argument)
        {
            var expression = VisitExpression(argument.Expression);
            return SF.Argument(expression);
        }

        public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            SMS.Add(node);
            return base.VisitVariableDeclarator(node);
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            SMS.Update(((IdentifierNameSyntax)node.Left).Identifier.Text, node.Right);
            return base.VisitAssignmentExpression(node);
        }

        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            if (node.IsKind(SK.PostIncrementExpression))
            {
                var identifier = ((IdentifierNameSyntax)node.Operand).Identifier.Text;
                SMS.Update(identifier, node);
            }
            else
            {
                throw new NotImplementedException("Unsupported PostfixUnaryExpression: " + node.Kind());
            }
            return base.VisitPostfixUnaryExpression(node);
        }

        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if (node.IsKind(SK.PreIncrementExpression))
            {
                var identifier = ((IdentifierNameSyntax)node.Operand).Identifier.Text;
                SMS.Update(identifier, node);
            }
            else if (node.IsKind(SK.UnaryMinusExpression))
            {
                return base.VisitPrefixUnaryExpression(node);
            }
            else
            {
                throw new NotImplementedException("Unsupported PrefixUnaryExpression: " + node.Kind());
            }
            return base.VisitPrefixUnaryExpression(node);
        }

        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
        {
            return MakeIfStatement(LoopIterations, node.Statement, SF.IfStatement(node.Condition, node.Statement));
        }

        ////for (int i = 0; i<length; i++)
        ////{
        ////    // inside-for
        ////}
        ////// next

        ////int i = 0;
        ////if (i<length)
        ////{
        ////    // inside-for
        ////    i++;
        ////    if (i<length)
        ////    {
        ////        // inside-for
        ////        i++;
        ////    }
        ////}
        ////// next
        public override SyntaxNode VisitForStatement(ForStatementSyntax node)
        {
            var incrementator = node.Incrementors.Single();

            var forStatementsBlock = SF.Block(JoinStatements(node.Statement, SF.ExpressionStatement(incrementator)));

            var ifStatement = MakeIfStatement(LoopIterations, forStatementsBlock, SF.IfStatement(node.Condition, forStatementsBlock));

            return SF.Block(SF.LocalDeclarationStatement(node.Declaration), ifStatement);
        }

        private IfStatementSyntax MakeIfStatement(uint count, StatementSyntax statement, IfStatementSyntax ifStatement, ElseClauseSyntax elseClause = null)
        {
            count--;
            if (count > 0)
            {
                if (elseClause == null)
                {
                    return SF.IfStatement(ifStatement.Condition, SF.Block(JoinStatements(statement, MakeIfStatement(count, statement, ifStatement))));
                }
                return SF.IfStatement(ifStatement.Condition, SF.Block(JoinStatements(statement, MakeIfStatement(count, statement, ifStatement, elseClause))), elseClause);
            }
            return ifStatement;
        }

        private IEnumerable<StatementSyntax> JoinStatements(StatementSyntax firstStatements, StatementSyntax secondStatements)
        {
            var statements = new List<StatementSyntax>();

            if (firstStatements.IsKind(SK.Block))
            {
                statements.AddRange(firstStatements.ChildNodes().OfType<StatementSyntax>());
            }
            else
            {
                statements.Add(firstStatements);
            }

            if (secondStatements.IsKind(SK.Block))
            {
                statements.AddRange(secondStatements.ChildNodes().OfType<StatementSyntax>());
            }
            else
            {
                statements.Add(secondStatements);
            }

            return statements;
        }
    }
}