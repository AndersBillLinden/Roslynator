﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class SimplifyNullableOfTRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, GenericNameSyntax genericName)
        {
            if (!genericName.IsParentKind(SyntaxKind.QualifiedName, SyntaxKind.UsingDirective))
            {
                TypeArgumentListSyntax typeArgumentList = genericName.TypeArgumentList;

                if (typeArgumentList != null)
                {
                    SeparatedSyntaxList<TypeSyntax> arguments = typeArgumentList.Arguments;

                    if (arguments.Count == 1
                        && !arguments[0].IsKind(SyntaxKind.OmittedTypeArgument))
                    {
                        var namedTypeSymbol = context.SemanticModel.GetSymbol(genericName, context.CancellationToken) as INamedTypeSymbol;

                        if (namedTypeSymbol?.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                        {
                            context.ReportDiagnostic(
                                DiagnosticDescriptors.SimplifyNullableOfT,
                                genericName.GetLocation());
                        }
                    }
                }
            }
        }

        public static void Analyze(SyntaxNodeAnalysisContext context, QualifiedNameSyntax qualifiedName)
        {
            if (!qualifiedName.IsParentKind(SyntaxKind.UsingDirective))
            {
                var namedTypeSymbol = context.SemanticModel.GetSymbol(qualifiedName, context.CancellationToken) as INamedTypeSymbol;

                if (namedTypeSymbol?.SupportsPredefinedType() == false
                    && namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.SimplifyNullableOfT, qualifiedName.GetLocation());
                }
            }
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            TypeSyntax type,
            TypeSyntax nullableType,
            CancellationToken cancellationToken)
        {
            TypeSyntax newType = NullableType(nullableType.WithoutTrivia(), QuestionToken())
                .WithTriviaFrom(type)
                .WithFormatterAnnotation();

            return await document.ReplaceNodeAsync(type, newType).ConfigureAwait(false);
        }
    }
}
