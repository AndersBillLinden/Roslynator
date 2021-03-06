﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings;

namespace Roslynator.CSharp.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConditionalExpressionCodeFixProvider))]
    [Shared]
    public class ConditionalExpressionCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticIdentifiers.WrapConditionalExpressionConditionInParentheses,
                    DiagnosticIdentifiers.ReplaceConditionalExpressionWithCoalesceExpression,
                    DiagnosticIdentifiers.SimplifyConditionalExpression);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            ConditionalExpressionSyntax conditionalExpression = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<ConditionalExpressionSyntax>();

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case DiagnosticIdentifiers.WrapConditionalExpressionConditionInParentheses:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Wrap condition in parentheses",
                                cancellationToken => WrapConditionalExpressionConditionInParenthesesRefactoring.RefactorAsync(context.Document, conditionalExpression, cancellationToken),
                                diagnostic.Id + EquivalenceKeySuffix);

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                    case DiagnosticIdentifiers.ReplaceConditionalExpressionWithCoalesceExpression:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Replace ?: with ??",
                                cancellationToken =>
                                {
                                    return ReplaceConditionalExpressionWithCoalesceExpressionRefactoring.RefactorAsync(
                                        context.Document,
                                        conditionalExpression,
                                        cancellationToken);
                                },
                                diagnostic.Id + EquivalenceKeySuffix);

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                    case DiagnosticIdentifiers.SimplifyConditionalExpression:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Simplify conditional expression",
                                cancellationToken =>
                                {
                                    return SimplifyConditionalExpressionRefactoring.RefactorAsync(
                                        context.Document,
                                        conditionalExpression,
                                        cancellationToken);
                                },
                                diagnostic.Id + EquivalenceKeySuffix);

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                }
            }
        }
    }
}
