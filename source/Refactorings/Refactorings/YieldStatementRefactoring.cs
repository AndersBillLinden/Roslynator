﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings.ReplaceStatementWithIf;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class YieldStatementRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, YieldStatementSyntax yieldStatement)
        {
            if (context.IsAnyRefactoringEnabled(
                    RefactoringIdentifiers.ChangeMemberTypeAccordingToYieldReturnExpression,
                    RefactoringIdentifiers.AddCastExpression,
                    RefactoringIdentifiers.CallToMethod)
                && yieldStatement.IsYieldReturn()
                && yieldStatement.Expression != null)
            {
                MemberDeclarationSyntax containingMember = ReturnExpressionRefactoring.GetContainingMethodOrPropertyOrIndexer(yieldStatement.Expression);

                if (containingMember != null)
                {
                    TypeSyntax memberType = ReturnExpressionRefactoring.GetMemberType(containingMember);

                    if (memberType != null)
                    {
                        SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                        ITypeSymbol memberTypeSymbol = semanticModel
                            .GetTypeInfo(memberType, context.CancellationToken)
                            .Type;

                        if (memberTypeSymbol?.SpecialType != SpecialType.System_Collections_IEnumerable)
                        {
                            ITypeSymbol typeSymbol = semanticModel
                                .GetTypeInfo(yieldStatement.Expression, context.CancellationToken)
                                .Type;

                            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ChangeMemberTypeAccordingToYieldReturnExpression)
                                && typeSymbol?.IsErrorType() == false
                                && !typeSymbol.IsVoid()
                                && (memberTypeSymbol == null
                                    || memberTypeSymbol.IsErrorType()
                                    || !memberTypeSymbol.IsConstructedFromIEnumerableOfT()
                                    || !((INamedTypeSymbol)memberTypeSymbol).TypeArguments[0].Equals(typeSymbol)))
                            {
                                TypeSyntax newType = QualifiedName(
                                    ParseName(MetadataNames.System_Collections_Generic),
                                    GenericName(
                                        Identifier("IEnumerable"),
                                        TypeArgumentList(
                                            SingletonSeparatedList(
                                                CSharpFactory.Type(typeSymbol)))));

                                context.RegisterRefactoring(
                                    $"Change {ReturnExpressionRefactoring.GetText(containingMember)} type to 'IEnumerable<{typeSymbol.ToMinimalDisplayString(semanticModel, memberType.Span.Start, DefaultSymbolDisplayFormat.Value)}>'",
                                    cancellationToken =>
                                    {
                                        return ChangeTypeRefactoring.ChangeTypeAsync(
                                            context.Document,
                                            memberType,
                                            newType,
                                            cancellationToken);
                                    });
                            }

                            if (context.IsAnyRefactoringEnabled(RefactoringIdentifiers.AddCastExpression, RefactoringIdentifiers.CallToMethod)
                                && yieldStatement.Expression.Span.Contains(context.Span)
                                && memberTypeSymbol?.IsNamedType() == true)
                            {
                                var namedTypeSymbol = (INamedTypeSymbol)memberTypeSymbol;

                                if (namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                                {
                                    ITypeSymbol argumentSymbol = namedTypeSymbol.TypeArguments[0];

                                    if (argumentSymbol != typeSymbol)
                                    {
                                        ModifyExpressionRefactoring.ComputeRefactoring(
                                           context,
                                           yieldStatement.Expression,
                                           argumentSymbol,
                                           semanticModel);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceStatementWithIfStatement)
                && context.Span.IsBetweenSpans(yieldStatement))
            {
                var refactoring = new ReplaceYieldStatementWithIfStatementRefactoring();
                await refactoring.ComputeRefactoringAsync(context, yieldStatement).ConfigureAwait(false);
            }
        }
    }
}
