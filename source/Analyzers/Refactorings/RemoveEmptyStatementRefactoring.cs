﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Refactorings
{
    internal static class RemoveEmptyStatementRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, SyntaxNode emptyStatement)
        {
            SyntaxNode parent = emptyStatement.Parent;

            if (parent != null
                && !EmbeddedStatement.CanContainEmbeddedStatement(parent))
            {
                context.ReportDiagnostic(DiagnosticDescriptors.RemoveEmptyStatement, emptyStatement.GetLocation());
            }
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            EmptyStatementSyntax emptyStatement,
            CancellationToken cancellationToken)
        {
            return await document.RemoveNodeAsync(emptyStatement, SyntaxRemoveOptions.KeepExteriorTrivia).ConfigureAwait(false);
        }
    }
}
