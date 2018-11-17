using Frisia.Solver;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using SK = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Frisia.Rewriter.Tests
{
    [TestCaseOrderer("Frisia.Rewriter.Tests.PriorityOrderer", "Frisia.Rewriter.Tests")]
    public class FrisiaSyntaxRewriterTests : TestsBase
    {
        // Test cases should be rewritten before running other tests.
        [Fact, TestPriority(1)]
        public void Rewritten_TestCases_should_exists()
        {
            foreach (var tc in TestCases)
            {
                // Debugging
                if (!string.IsNullOrEmpty(TestToDebug) && tc.Key != TestToDebug) continue;

                var timer = new Stopwatch();

                // Arrange
                var rootNode = CSharpSyntaxTree.ParseText(tc.Value).GetRoot();
                var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var m in methods)
                {
                    var conditions = new List<ExpressionSyntax>();
                    var solver = new Z3Solver();
                    var sms = new SymbolicMemoryState(m.ParameterList.Parameters);
                    var rewriter = new FrisiaSyntaxRewriter(conditions, m.ParameterList.Parameters, sms, solver, null, LoopIterations, visitUnsatPaths: true, logFoundBranches: false);

                    // Act
                    timer.Start();
                    var rewrittenNode = (MethodDeclarationSyntax)rewriter.Visit(m);
                    timer.Stop();

                    rootNode = rootNode.ReplaceNode(m, rewrittenNode);
                }

                var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\")) + "TestCasesRewritten\\";
                var path = $"{directory}{tc.Key}";
                var rewrittenNodeText = $"// This code was generated by Frisia.Rewriter.{Environment.NewLine}// Date: {DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}{Environment.NewLine}// Time: {timer.Elapsed}{Environment.NewLine}{Environment.NewLine}{rootNode.ToFullString()}";

                File.WriteAllText(path, rewrittenNodeText);

                // Assert
                Assert.True(File.Exists(path));
            }
        }

        /// <summary>
        /// INPUT:
        /// 
        /// {
        ///     {   // node
        ///         [a]
        ///     }
        ///     {
        ///         [b]
        ///     }
        ///     [c]
        /// }
        /// OUTPUT:
        /// {
        ///     [a]
        ///     {
        ///         [b]
        ///     }
        ///     [c]
        /// }
        /// 
        /// </summary>
        [Fact]
        public void Single_child_in_block_with_parent_of_kind_block_should_be_moved_one_level_up()
        {
            // Arrange
            var rootNode = CSharpSyntaxTree.ParseText(@"
            using System;

            namespace Frisia.CodeReduction
            {
                class Program
                {
                    public static void Method(int a)
                    {
                        {
                            {
                                {
                                    Console.WriteLine (""First content in block."");
                                }
                                {
                                    Console.WriteLine (""Second content in block."");
                                }
                            }
                        }
                    }
                }
            }
            ").GetRoot();
            var m = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var conditions = new List<ExpressionSyntax>();
            var solver = new Z3Solver();
            var sms = new SymbolicMemoryState(m.ParameterList.Parameters);
            var rewriter = new FrisiaSyntaxRewriter(conditions, m.ParameterList.Parameters, sms, solver, null, LoopIterations, visitUnsatPaths: true, logFoundBranches: false);

            // Act
            var rewrittenNode = rewriter.Visit(rootNode);

            var blockStatementsWithParentBlockAndSingleChild = rootNode.DescendantNodes().OfType<BlockSyntax>()
                .Where(b => b.Parent.Kind() == SK.Block && b.ChildNodes().Count() == 1);
            var rewrittenBlockStatementsWithParentBlockAndSingleChild = rewrittenNode.DescendantNodes().OfType<BlockSyntax>()
                .Where(b => b.Parent.Kind() == SK.Block && b.ChildNodes().Count() == 1);

            // Assert
            Assert.NotEmpty(blockStatementsWithParentBlockAndSingleChild);
            Assert.Empty(rewrittenBlockStatementsWithParentBlockAndSingleChild);
        }

        [Fact]
        public void Each_block_statement_should_contain_single_statement()
        {
            foreach (var tc in TestCases)
            {
                try
                {
                    // Debugging
                    if (!string.IsNullOrEmpty(TestToDebug) && tc.Key != TestToDebug) continue;

                    // Arrange
                    var rootNode = CSharpSyntaxTree.ParseText(tc.Value).GetRoot();
                    var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var m in methods)
                    {
                        var conditions = new List<ExpressionSyntax>();
                        var solver = new Z3Solver();
                        var sms = new SymbolicMemoryState(m.ParameterList.Parameters);
                        var rewriter = new FrisiaSyntaxRewriter(conditions, m.ParameterList.Parameters, sms, solver, null, LoopIterations, visitUnsatPaths: true, logFoundBranches: false);

                        // Act

                        var rewrittenNode = rewriter.Visit(m);

                        var blocks = rewrittenNode.DescendantNodes().OfType<BlockSyntax>();

                        // Assert
                        Assert.All(blocks, b => b.ChildNodes().OfType<StatementSyntax>().Where(x => x.Kind() != SK.ExpressionStatement &&
                                                                                                    x.Kind() != SK.LocalDeclarationStatement).SingleOrDefault());
                    }
                }
                catch (Exception ex)
                {
                    throw new TestCaseFailedException(tc.Key, ex);
                }
            }
        }

        /// <summary>
        /// INPUT:
        /// for (int i = 0; i < n; i++)
        /// {
        ///     [for]
        /// }
        /// 
        /// OUTPUT:
        /// int i = 0;
        /// if (i < n)
        /// {      
        ///     [for]
        ///     i++;
        ///     if (i < n)
        ///     {       
        ///         [for]
        ///         i++;
        ///         if (i < n)
        ///         {
        ///             [for]
        ///             i++;
        ///         }
        ///     }
        /// }
        /// </summary>
        [Fact]
        public void For_loop_should_be_converted_to_if_statements()
        {
            foreach (var tc in TestCases)
            {
                try
                {
                    // Debugging
                    if (!string.IsNullOrEmpty(TestToDebug) && tc.Key != TestToDebug) continue;

                    // Arrange
                    var rootNode = CSharpSyntaxTree.ParseText(tc.Value).GetRoot();
                    var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var m in methods)
                    {
                        var conditions = new List<ExpressionSyntax>();
                        var solver = new Z3Solver();
                        var sms = new SymbolicMemoryState(m.ParameterList.Parameters);
                        var rewriter = new FrisiaSyntaxRewriter(conditions, m.ParameterList.Parameters, sms, solver, null, LoopIterations, visitUnsatPaths: true, logFoundBranches: false);

                        // Act
                        var rewrittenNode = rewriter.Visit(m);

                        //Assert
                        var forStatements = rootNode.DescendantNodes().OfType<ForStatementSyntax>().ToArray();
                        foreach (var forStatement in forStatements)
                        {
                            var forCondition = (BinaryExpressionSyntax)forStatement.Condition;
                            var rewrittenIfStatements = rewrittenNode.DescendantNodes().OfType<IfStatementSyntax>().Where(s => s.Condition.ToString() == forCondition.ToString()).ToArray();
                            Assert.NotEmpty(rewrittenIfStatements);
                        }

                        var rewrittenForStatements = rewrittenNode.DescendantNodes().OfType<ForStatementSyntax>().ToArray();
                        Assert.Empty(rewrittenForStatements);
                    }
                }
                catch (Exception ex)
                {
                    throw new TestCaseFailedException(tc.Key, ex);
                }
            }
        }

        /// <summary>
        /// INPUT:
        /// for (int i = 0; i < n; i++)
        /// {
        ///     [for]
        /// }
        /// 
        /// OUTPUT:
        /// int i = 0;
        /// if (i < n)
        /// {      
        ///     [for]
        ///     i++;
        ///     if (i < n)
        ///     {       
        ///         [for]
        ///         i++;
        ///         if (i < n)
        ///         {
        ///             [for]
        ///             i++;
        ///         }
        ///     }
        /// }
        /// </summary>
        [Fact]
        public void While_loop_should_be_converted_to_if_statements()
        {
            foreach (var tc in TestCases)
            {
                try
                {
                    // Debugging
                    if (!string.IsNullOrEmpty(TestToDebug) && tc.Key != TestToDebug) continue;

                    // Arrange
                    var rootNode = CSharpSyntaxTree.ParseText(tc.Value).GetRoot();
                    var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var m in methods)
                    {
                        var conditions = new List<ExpressionSyntax>();
                        var solver = new Z3Solver();
                        var sms = new SymbolicMemoryState(m.ParameterList.Parameters);
                        var rewriter = new FrisiaSyntaxRewriter(conditions, m.ParameterList.Parameters, sms, solver, null, LoopIterations, visitUnsatPaths: true, logFoundBranches: false);

                        // Act
                        var rewrittenNode = rewriter.Visit(m);

                        //Assert
                        var whileStatements = rootNode.DescendantNodes().OfType<WhileStatementSyntax>().ToArray();
                        foreach (var whileStatement in whileStatements)
                        {
                            var whileCondition = (BinaryExpressionSyntax)whileStatement.Condition;
                            var rewrittenIfStatements = rewrittenNode.DescendantNodes().OfType<IfStatementSyntax>().Where(s => s.Condition.ToString() == whileCondition.ToString()).ToArray();
                            Assert.NotEmpty(rewrittenIfStatements);
                        }

                        var rewrittenWhileStatements = rewrittenNode.DescendantNodes().OfType<WhileStatementSyntax>().ToArray();
                        Assert.Empty(rewrittenWhileStatements);
                    }
                }
                catch (Exception ex)
                {
                    throw new TestCaseFailedException(tc.Key, ex);
                }
            }
        }

        /// <summary>
        /// INPUT:
        /// if (a && b)
        /// {
        ///     [a && b]
        /// }
        /// else
        /// {
        ///     ![a && b]
        /// }
        /// 
        /// OUTPUT:
        /// if (a)
        /// {
        ///     if (b)
        ///     {
        ///         [a && b]
        ///     }
        ///     else
        ///     {
        ///         ![a && b]
        ///     }
        /// }
        /// else
        /// {
        ///     ![a && b]
        /// }
        /// </summary>
        [Fact]
        public void If_statement_should_not_contain_logical_AND_expression()
        {
            foreach (var tc in TestCases)
            {
                try
                {
                    // Debugging
                    if (!string.IsNullOrEmpty(TestToDebug) && tc.Key != TestToDebug) continue;

                    // Arrange
                    var rootNode = CSharpSyntaxTree.ParseText(tc.Value).GetRoot();
                    var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var m in methods)
                    {
                        var conditions = new List<ExpressionSyntax>();
                        var solver = new Z3Solver();
                        var sms = new SymbolicMemoryState(m.ParameterList.Parameters);
                        var rewriter = new FrisiaSyntaxRewriter(conditions, m.ParameterList.Parameters, sms, solver, null, LoopIterations, visitUnsatPaths: true, logFoundBranches: false);

                        // Act
                        var rewrittenNode = rewriter.Visit(m);

                        //Assert
                        var rewrittenIfStatements = rewrittenNode.DescendantNodes().OfType<IfStatementSyntax>();

                        Assert.All(rewrittenIfStatements, s => Assert.True(s.Condition.Kind() != SK.LogicalAndExpression));
                    }
                }
                catch (Exception ex)
                {
                    throw new TestCaseFailedException(tc.Key, ex);
                }
            }
        }

        /// <summary>
        /// INPUT:
        /// if (a || b)
        /// {
        ///     [a || b]
        /// }
        /// else
        /// {
        ///     ![a || b]
        /// }
        /// 
        /// OUTPUT:
        /// if (a)
        /// {
        ///     [a || b]
        /// }
        /// else
        /// {
        ///     if (b)
        ///     {
        ///         [a || b]
        ///     }
        ///     else
        ///     {
        ///         ![a || b]
        ///     }
        /// }
        /// </summary>
        [Fact]
        public void If_statement_should_not_contain_logical_OR_expression()
        {
            foreach (var tc in TestCases)
            {
                try
                {
                    // Debugging
                    if (!string.IsNullOrEmpty(TestToDebug) && tc.Key != TestToDebug) continue;

                    // Arrange
                    var rootNode = CSharpSyntaxTree.ParseText(tc.Value).GetRoot();
                    var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var m in methods)
                    {
                        var conditions = new List<ExpressionSyntax>();
                        var solver = new Z3Solver();
                        var sms = new SymbolicMemoryState(m.ParameterList.Parameters);
                        var rewriter = new FrisiaSyntaxRewriter(conditions, m.ParameterList.Parameters, sms, solver, null, LoopIterations, visitUnsatPaths: true, logFoundBranches: false);

                        // Act
                        var rewrittenNode = rewriter.Visit(m);

                        //Assert
                        var rewrittenIfStatements = rewrittenNode.DescendantNodes().OfType<IfStatementSyntax>();

                        Assert.All(rewrittenIfStatements, s => Assert.True(s.Condition.Kind() != SK.LogicalOrExpression));
                    }
                }
                catch (Exception ex)
                {
                    throw new TestCaseFailedException(tc.Key, ex);
                }
            }
        }

        /// <summary>
        /// INPUT:
        /// if (a && b)
        /// {
        ///     [a && b]
        /// }
        /// else
        /// {
        ///     ![a && b]
        /// }
        /// 
        /// OUTPUT:
        /// if (a)
        /// {
        ///     if (b)
        ///     {
        ///         [a && b]
        ///     }
        ///     else
        ///     {
        ///         ![a && b]
        ///     }   
        /// }
        /// else
        /// {
        ///     ![a && b]
        /// }
        /// </summary>
        [Fact]
        public void AND_expressions_should_be_replaced_correctly()
        {
            // Arrange
            var rootNode = CSharpSyntaxTree.ParseText(@"
            using System;

            namespace Frisia.CodeReduction
            {
                class Program
                {
                    public static void Method(int a, int b)
                    {
                        if (a > 0 && b > 0)
                        {
                            Console.WriteLine(""a > 0 && b > 0"");
                                
                            if (a > 1 && b > 1)
                            {
                                Console.WriteLine(""a > 1 && b > 1"");

                                if (a > 2 && b > 2)
                                {
                                    Console.WriteLine(""a > 2 && b > 2"");
                                }
                                else
                                {
                                    Console.WriteLine(""!(a > 2 && b > 2)"");
                                }
                            }
                            else
                            {
                                Console.WriteLine(""!(a > 1 && b > 1)"");

                                if (a > 2 && b > 2)
                                {
                                    Console.WriteLine(""a > 2 && b > 2"");
                                }
                                else
                                {
                                    Console.WriteLine(""!(a > 2 && b > 2)"");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine(""!(a > 0 && b > 0)"");

                            if (a < 1 && b < 1)
                            {
                                Console.WriteLine(""a < 1 && b < 1"");

                                if (a < 2 && b < 2)
                                {
                                    Console.WriteLine(""a < 2 && b < 2"");
                                }
                                else
                                {
                                    Console.WriteLine(""!(a < 2 && b < 2)"");
                                }
                            }
                            else
                            {
                                Console.WriteLine(""!(a < 1 && b < 1)"");

                                if (a < 2 && b < 2)
                                {
                                    Console.WriteLine(""a < 2 && b < 2"");
                                }
                                else
                                {
                                    Console.WriteLine(""!(a < 2 && b < 2)"");
                                }
                            }
                        }
                    }
                }
            }
            ").GetRoot();

            var methods = rootNode.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var m in methods)
            {
                var conditions = new List<ExpressionSyntax>();
                var solver = new Z3Solver();
                var sms = new SymbolicMemoryState(m.ParameterList.Parameters);
                var rewriter = new FrisiaSyntaxRewriter(conditions, m.ParameterList.Parameters, sms, solver, null, LoopIterations, visitUnsatPaths: true, logFoundBranches: false);


                // Act
                var rewrittenNode = rewriter.Visit(rootNode);

                //var ifStatements = rootNode.DescendantNodes().OfType<IfStatementSyntax>().ToArray();
                var rewrittenIfStatements = rewrittenNode.DescendantNodes().OfType<IfStatementSyntax>().ToArray();

                // Assert
                Assert.Equal(26, rewrittenIfStatements.Length);
            }
        }
    }
}
