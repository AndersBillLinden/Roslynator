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
    internal static class RemoveUnnecessaryCaseLabelRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, SwitchSectionSyntax switchSection)
        {
            if (switchSection.IsParentKind(SyntaxKind.SwitchStatement))
            {
                SyntaxList<SwitchLabelSyntax> labels = switchSection.Labels;

                if (labels.Count > 1
                    && labels.Any(SyntaxKind.DefaultSwitchLabel))
                {
                    foreach (SwitchLabelSyntax label in labels)
                    {
                        if (!label.IsKind(SyntaxKind.DefaultSwitchLabel)
                            && label
                                .DescendantTrivia(label.Span)
                                .All(f => f.IsWhitespaceOrEndOfLineTrivia()))
                        {
                            context.ReportDiagnostic(
                                DiagnosticDescriptors.RemoveUnnecessaryCaseLabel,
                                label.GetLocation());
                        }
                    }
                }
            }
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            CaseSwitchLabelSyntax label,
            CancellationToken cancellationToken)
        {
            var switchSection = (SwitchSectionSyntax)label.Parent;

            SwitchSectionSyntax newNode = switchSection.RemoveNode(label, GetRemoveOptions(label))
                .WithFormatterAnnotation();

            return await document.ReplaceNodeAsync(switchSection, newNode).ConfigureAwait(false);
        }

        private static SyntaxRemoveOptions GetRemoveOptions(CaseSwitchLabelSyntax label)
        {
            if (label.GetLeadingTrivia().All(f => f.IsWhitespaceOrEndOfLineTrivia())
                && label.GetTrailingTrivia().All(f => f.IsWhitespaceOrEndOfLineTrivia()))
            {
                return SyntaxRemoveOptions.KeepNoTrivia;
            }

            return SyntaxRemoveOptions.KeepExteriorTrivia;
        }
    }
}
