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
    internal static class RemoveRedundantConstructorRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, ConstructorDeclarationSyntax constructor)
        {
            if (constructor.ParameterList?.Parameters.Any() == false
                && constructor.Body?.Statements.Any() == false)
            {
                SyntaxTokenList modifiers = constructor.Modifiers;

                if (modifiers.Contains(SyntaxKind.PublicKeyword)
                    && !modifiers.Contains(SyntaxKind.StaticKeyword))
                {
                    ConstructorInitializerSyntax initializer = constructor.Initializer;

                    if (initializer == null
                        || initializer.ArgumentList?.Arguments.Any() == false)
                    {
                        if (IsSingleInstanceConstructor(constructor)
                            && constructor
                                .DescendantTrivia(constructor.Span)
                                .All(f => f.IsWhitespaceOrEndOfLineTrivia()))
                        {
                            context.ReportDiagnostic(DiagnosticDescriptors.RemoveRedundantConstructor, constructor.GetLocation());
                        }
                    }
                }
            }
        }

        private static bool IsSingleInstanceConstructor(ConstructorDeclarationSyntax constructor)
        {
            var parent = constructor.Parent as MemberDeclarationSyntax;

            return parent != null
                && parent
                    .GetMembers()
                    .OfType<ConstructorDeclarationSyntax>()
                    .All(f => f.Equals(constructor)
                        || f.Modifiers.Contains(SyntaxKind.StaticKeyword));
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            ConstructorDeclarationSyntax constructorDeclaration,
            CancellationToken cancellationToken)
        {
            return await SyntaxRemover.RemoveMemberAsync(document, constructorDeclaration, cancellationToken).ConfigureAwait(false);
        }
    }
}
