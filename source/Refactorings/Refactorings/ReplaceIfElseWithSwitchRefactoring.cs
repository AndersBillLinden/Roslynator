﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ReplaceIfElseWithSwitchRefactoring
    {
        public static void ComputeRefactoring(RefactoringContext context, SelectedStatementsInfo info)
        {
            if (info.IsSingleSelected)
            {
                StatementSyntax[] statements = info.SelectedNodes().ToArray();

                StatementSyntax first = statements[0];

                if (first.IsKind(SyntaxKind.IfStatement))
                {
                    var ifStatement = (IfStatementSyntax)first;

                    if (IfElseChain.IsTopmostIf(ifStatement)
                        && CanBeReplacedWithSwitch(ifStatement))
                    {
                        string title = (IfElseChain.IsPartOfChain(ifStatement))
                            ? "Replace if-else with switch"
                            : "Replace if with switch";

                        context.RegisterRefactoring(
                            title,
                            cancellationToken =>
                            {
                                return RefactorAsync(
                                    context.Document,
                                    ifStatement,
                                    cancellationToken);
                            });
                    }
                }
            }
        }

        private static bool CanBeReplacedWithSwitch(IfStatementSyntax ifStatement)
        {
            foreach (SyntaxNode node in IfElseChain.GetChain(ifStatement))
            {
                if (node.IsKind(SyntaxKind.IfStatement))
                {
                    ifStatement = (IfStatementSyntax)node;

                    var condition = ifStatement.Condition as BinaryExpressionSyntax;

                    if (condition == null
                        || !IsValidCondition(condition, null))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsValidCondition(BinaryExpressionSyntax binaryExpression, ExpressionSyntax switchExpression)
        {
            if (binaryExpression.IsKind(SyntaxKind.EqualsExpression))
            {
                return switchExpression == null
                    || binaryExpression.Left?.IsEquivalentTo(switchExpression, topLevel: false) == true;
            }
            else if (binaryExpression.IsKind(SyntaxKind.LogicalOrExpression))
            {
                ExpressionSyntax right = binaryExpression.Right;

                if (right?.IsKind(SyntaxKind.EqualsExpression) == true)
                {
                    var equalsExpression = (BinaryExpressionSyntax)right;

                    if (switchExpression == null)
                        switchExpression = equalsExpression.Left;

                    if (equalsExpression.Left?.IsEquivalentTo(switchExpression, topLevel: false) == true)
                    {
                        binaryExpression = binaryExpression.Left as BinaryExpressionSyntax;

                        if (binaryExpression != null)
                        {
                            return IsValidCondition(binaryExpression, switchExpression);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return false;
        }

        private static async Task<Document> RefactorAsync(
            Document document,
            IfStatementSyntax ifStatement,
            CancellationToken cancellationToken)
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            SwitchStatementSyntax switchStatement = SwitchStatement(
                GetSwitchExpression(ifStatement),
                List(CreateSwitchSections(ifStatement)));

            SyntaxNode newRoot = root.ReplaceNode(
                ifStatement,
                switchStatement
                    .WithTriviaFrom(ifStatement)
                    .WithFormatterAnnotation());

            return document.WithSyntaxRoot(newRoot);
        }

        private static ExpressionSyntax GetSwitchExpression(IfStatementSyntax ifStatement)
        {
            var condition = (BinaryExpressionSyntax)ifStatement.Condition;

            if (condition.IsKind(SyntaxKind.LogicalOrExpression))
            {
                var right = (BinaryExpressionSyntax)condition.Right;

                return right.Left;
            }

            return condition.Left;
        }

        private static IEnumerable<SwitchSectionSyntax> CreateSwitchSections(IfStatementSyntax ifStatement)
        {
            foreach (SyntaxNode node in IfElseChain.GetChain(ifStatement))
            {
                if (node.IsKind(SyntaxKind.IfStatement))
                {
                    ifStatement = (IfStatementSyntax)node;

                    var condition = ifStatement.Condition as BinaryExpressionSyntax;

                    List<SwitchLabelSyntax> labels = CreateSwitchLabels(condition, new List<SwitchLabelSyntax>());
                    labels.Reverse();

                    SwitchSectionSyntax section = SwitchSection(
                        List(labels),
                        AddBreakStatementIfNecessary(ifStatement.Statement));

                    yield return section;
                }
                else
                {
                    var elseClause = (ElseClauseSyntax)node;

                    yield return DefaultSwitchSection(AddBreakStatementIfNecessary(elseClause.Statement));
                }
            }
        }

        private static SyntaxList<StatementSyntax> AddBreakStatementIfNecessary(StatementSyntax statement)
        {
            if (statement.IsKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)statement;

                SyntaxList<StatementSyntax> statements = block.Statements;

                if (statements.Any()
                    && IsJumpStatement(statements.Last()))
                {
                    return SingletonList<StatementSyntax>(block);
                }
                else
                {
                    return SingletonList<StatementSyntax>(block.AddStatements(BreakStatement()));
                }
            }
            else
            {
                if (IsJumpStatement(statement))
                {
                    return SingletonList(statement);
                }
                else
                {
                    return SingletonList<StatementSyntax>(Block(statement, BreakStatement()));
                }
            }
        }

        private static bool IsJumpStatement(StatementSyntax statement)
        {
            return statement.IsKind(
                SyntaxKind.BreakStatement,
                SyntaxKind.GotoCaseStatement,
                SyntaxKind.ReturnStatement,
                SyntaxKind.ThrowStatement);
        }

        private static List<SwitchLabelSyntax> CreateSwitchLabels(BinaryExpressionSyntax binaryExpression, List<SwitchLabelSyntax> labels)
        {
            if (binaryExpression.IsKind(SyntaxKind.EqualsExpression))
            {
                labels.Add(CaseSwitchLabel(binaryExpression.Right));
            }
            else
            {
                var equalsExpression = (BinaryExpressionSyntax)binaryExpression.Right;

                labels.Add(CaseSwitchLabel(equalsExpression.Right));

                if (binaryExpression.IsKind(SyntaxKind.LogicalOrExpression))
                    return CreateSwitchLabels((BinaryExpressionSyntax)binaryExpression.Left, labels);
            }

            return labels;
        }
    }
}