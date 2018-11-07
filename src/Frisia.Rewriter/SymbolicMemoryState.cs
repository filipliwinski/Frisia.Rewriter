using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SK = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Frisia.Rewriter
{
    public class SymbolicMemoryState
    {
        public IDictionary<string, ExpressionSyntax> Variables { get; private set; }

        public SymbolicMemoryState(SymbolicMemoryState sms)
        {
            Variables = new Dictionary<string, ExpressionSyntax>(sms.Variables);
        }

        public SymbolicMemoryState(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            Variables = new Dictionary<string, ExpressionSyntax>();

            foreach (var p in parameters)
            {
                Variables.Add(p.Identifier.Text, SF.IdentifierName(p.Identifier));
            }
        }

        public void Add(VariableDeclaratorSyntax node)
        {
            var equalsValue = SF.EqualsValueClause(VisitExpression(node.Initializer.Value));
            node = SF.VariableDeclarator(node.Identifier, null, equalsValue);
            Variables.Add(node.Identifier.Text, node.Initializer.Value);
        }

        public void Update(string key, ExpressionSyntax node)
        {
            Variables[key] = VisitExpression(node);
        }

        private ExpressionSyntax VisitExpression(ExpressionSyntax node)
        {
            if (node is BinaryExpressionSyntax binaryExpresson)
            {
                var left = VisitExpression(binaryExpresson.Left);
                var right = VisitExpression(binaryExpresson.Right);
                return SF.BinaryExpression(node.Kind(), left, right);
            }
            if (node is IdentifierNameSyntax identifierName)
            {
                var value = Variables[identifierName.Identifier.Text];
                return value;
            }
            if (node is LiteralExpressionSyntax)
            {
                return node;
            }
            // Consider:
            // https://stackoverflow.com/a/3346729/2869093
            // https://stackoverflow.com/a/7812241/2869093
            if (node is PrefixUnaryExpressionSyntax prefixUnaryExpression)
            {
                if (prefixUnaryExpression.Operand is IdentifierNameSyntax operantIdentifierName)
                {
                    operantIdentifierName = (IdentifierNameSyntax)prefixUnaryExpression.Operand;
                    var value = Variables[operantIdentifierName.Identifier.Text];
                    switch (node.Kind())
                    {
                        case SK.PreDecrementExpression:
                            return SF.BinaryExpression(SK.SubtractExpression, value, SF.LiteralExpression(SK.NumericLiteralExpression, SF.Literal(1)));
                        case SK.PreIncrementExpression:
                            return SF.BinaryExpression(SK.AddExpression, value, SF.LiteralExpression(SK.NumericLiteralExpression, SF.Literal(1)));
                        case SK.UnaryMinusExpression:
                        case SK.UnaryPlusExpression:
                        default:
                            throw new NotImplementedException($"Unsupported expression type: {prefixUnaryExpression.Operand.GetType().Name} ({operantIdentifierName.Kind().ToString()})");
                    }
                }
                if (prefixUnaryExpression.Operand is LiteralExpressionSyntax)
                {
                    return node;
                }
                throw new NotImplementedException($"Unsupported expression type: {prefixUnaryExpression.Operand.GetType().Name}");
            }
            if (node is PostfixUnaryExpressionSyntax postfixUnaryExpression)
            {
                if (postfixUnaryExpression.Operand is IdentifierNameSyntax operandIdentifierName)
                {
                    var value = Variables[operandIdentifierName.Identifier.Text];
                    switch (node.Kind())
                    {
                        case SK.PostDecrementExpression:
                            return SF.BinaryExpression(SK.SubtractExpression, value, SF.LiteralExpression(SK.NumericLiteralExpression, SF.Literal(1)));
                        case SK.PostIncrementExpression:
                            return SF.BinaryExpression(SK.AddExpression, value, SF.LiteralExpression(SK.NumericLiteralExpression, SF.Literal(1)));
                        default:
                            throw new NotImplementedException($"Unsupported expression type: {postfixUnaryExpression.Operand.GetType().Name} ({operandIdentifierName.Kind().ToString()})");
                    }
                }
                if (postfixUnaryExpression.Operand is LiteralExpressionSyntax)
                {
                    return node;
                }
                throw new NotImplementedException($"Unsupported expression type: {postfixUnaryExpression.Operand.GetType().Name}");
            }
            if (node is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                return VisitExpression(parenthesizedExpression.Expression);
            }
            throw new NotImplementedException($"Unsupported expression type:  {node.GetType().Name}");
        }
    }
}