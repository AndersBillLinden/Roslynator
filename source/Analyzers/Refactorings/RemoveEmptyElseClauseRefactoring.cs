﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Refactorings
{
    internal static class RemoveEmptyElseClauseRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, ElseClauseSyntax elseClause)
        {
            StatementSyntax statement = elseClause.Statement;

            if (statement?.IsKind(SyntaxKind.Block) == true)
            {
                var block = (BlockSyntax)statement;

                if (!block.Statements.Any()
                    && elseClause
                        .DescendantTrivia(elseClause.Span)
                        .All(f => f.IsWhitespaceOrEndOfLineTrivia()))
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.RemoveEmptyElseClause, elseClause.GetLocation());
                }
            }
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            ElseClauseSyntax elseClause,
            CancellationToken cancellationToken)
        {
            if (elseClause.Parent?.IsKind(SyntaxKind.IfStatement) == true)
            {
                var ifStatement = (IfStatementSyntax)elseClause.Parent;
                StatementSyntax statement = ifStatement.Statement;

                if (statement?.GetTrailingTrivia().All(f => f.IsWhitespaceOrEndOfLineTrivia()) == true)
                {
                    IfStatementSyntax newIfStatement = ifStatement
                        .WithStatement(statement.WithTrailingTrivia(elseClause.GetTrailingTrivia()))
                        .WithElse(null);

                    return await document.ReplaceNodeAsync(ifStatement, newIfStatement).ConfigureAwait(false);
                }
            }

            return await document.RemoveNodeAsync(elseClause, SyntaxRemoveOptions.KeepExteriorTrivia).ConfigureAwait(false);
        }
    }
}
