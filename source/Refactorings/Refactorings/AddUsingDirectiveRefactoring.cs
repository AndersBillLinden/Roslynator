﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class AddUsingDirectiveRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, IdentifierNameSyntax identifierName)
        {
            SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

            INamespaceSymbol namespaceSymbol = null;

            SyntaxNode node = identifierName;
            SyntaxNode prevNode = null;

            while (node?.Parent?.IsKind(SyntaxKind.QualifiedName, SyntaxKind.AliasQualifiedName, SyntaxKind.SimpleMemberAccessExpression) == true)
            {
                ISymbol symbol = semanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol;

                if (symbol?.IsNamespace() == true)
                {
                    namespaceSymbol = (INamespaceSymbol)symbol;
                    prevNode = node;
                    node = node.Parent;
                }
                else
                {
                    break;
                }
            }

            node = prevNode;

            if (node?.IsParentKind(SyntaxKind.QualifiedName, SyntaxKind.AliasQualifiedName, SyntaxKind.SimpleMemberAccessExpression) == true
                && !node.IsDescendantOf(SyntaxKind.UsingDirective)
                && !SyntaxAnalyzer.IsUsingDirectiveInScope(node, namespaceSymbol, semanticModel, context.CancellationToken))
            {
                context.RegisterRefactoring(
                    $"using {namespaceSymbol.ToString()};",
                    cancellationToken => RefactorAsync(context.Document, node, namespaceSymbol, cancellationToken));
            }
        }

        private static async Task<Document> RefactorAsync(
            Document document,
            SyntaxNode node,
            INamespaceSymbol namespaceSymbol,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            UsingDirectiveSyntax usingDirective = UsingDirective(ParseName(namespaceSymbol.ToString()));

            CompilationUnitSyntax newRoot = ((CompilationUnitSyntax)root)
                .ReplaceNode(node.Parent, GetNewNode(node))
                .AddUsings(keepSingleLineCommentsOnTop: true, usings: usingDirective);

            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode GetNewNode(SyntaxNode node)
        {
            switch (node.Parent.Kind())
            {
                case SyntaxKind.QualifiedName:
                    {
                        var qualifiedName = (QualifiedNameSyntax)node.Parent;

                        return qualifiedName.Right.WithLeadingTrivia(node.GetLeadingTrivia());
                    }
                case SyntaxKind.SimpleMemberAccessExpression:
                    {
                        var memberAccess = (MemberAccessExpressionSyntax)node.Parent;

                        return memberAccess.Name.WithLeadingTrivia(node.GetLeadingTrivia());
                    }
            }

            Debug.Assert(false, node?.Parent?.Kind().ToString());

            return node;
        }
    }
}
