﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class CombineEnumerableWhereMethodChainRefactoring
    {
        private static DiagnosticDescriptor FadeOutDescriptor
        {
            get { return DiagnosticDescriptors.CombineEnumerableWhereMethodChainFadeOut; }
        }

        public static void Analyze(
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocation,
            MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Expression?.IsKind(SyntaxKind.InvocationExpression) == true)
            {
                var invocation2 = (InvocationExpressionSyntax)memberAccess.Expression;

                if (invocation2.ArgumentList?.Arguments.Count == 1
                    && invocation2.Expression?.IsKind(SyntaxKind.SimpleMemberAccessExpression) == true)
                {
                    var memberAccess2 = (MemberAccessExpressionSyntax)invocation2.Expression;

                    SemanticModel semanticModel = context.SemanticModel;
                    CancellationToken cancellationToken = context.CancellationToken;

                    if (string.Equals(memberAccess2.Name?.Identifier.ValueText, "Where", StringComparison.Ordinal))
                    {
                        IMethodSymbol methodSymbol = semanticModel.GetMethodSymbol(invocation2, cancellationToken);

                        if (methodSymbol != null)
                        {
                            IMethodSymbol reducedFrom = methodSymbol.ReducedFrom;

                            if (reducedFrom?.Name.Equals("Where", StringComparison.Ordinal) == true)
                            {
                                ImmutableArray<IParameterSymbol> parameters = methodSymbol.Parameters;

                                if (parameters.Length == 1
                                    && SemanticAnalyzer.IsContainedInEnumerable(reducedFrom, semanticModel)
                                    && reducedFrom.Parameters.First().Type.IsConstructedFromIEnumerableOfT())
                                {
                                    if (SemanticAnalyzer.IsPredicateFunc(
                                        parameters[0].Type,
                                        methodSymbol.TypeArguments[0],
                                        semanticModel))
                                    {
                                        if (SemanticAnalyzer.IsEnumerableWhereMethod(invocation, semanticModel, cancellationToken))
                                            Analyze(context, invocation, invocation2, memberAccess, memberAccess2);
                                    }
                                    else if (SemanticAnalyzer.IsPredicateFunc(
                                        parameters[0].Type,
                                        methodSymbol.TypeArguments[0],
                                        semanticModel.Compilation.GetSpecialType(SpecialType.System_Int32),
                                        semanticModel))
                                    {
                                        if (SemanticAnalyzer.IsEnumerableWhereMethodWithIndex(invocation, semanticModel, cancellationToken))
                                            Analyze(context, invocation, invocation2, memberAccess, memberAccess2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Analyze(
            SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocation1,
            InvocationExpressionSyntax invocation2,
            MemberAccessExpressionSyntax memberAccess1,
            MemberAccessExpressionSyntax memberAccess2)
        {
            ExpressionSyntax expression1 = invocation1.ArgumentList.Arguments.First().Expression;
            ExpressionSyntax expression2 = invocation2.ArgumentList.Arguments.First().Expression;

            if (CheckLambdas(expression1, expression2))
            {
                TextSpan span = TextSpan.FromBounds(memberAccess2.Name.Span.Start, invocation1.Span.End);

                if (!invocation1.ContainsDirectives(span))
                {
                    context.ReportDiagnostic(
                        DiagnosticDescriptors.CombineEnumerableWhereMethodChain,
                        Location.Create(invocation1.SyntaxTree, span));

                    FadeOut(context, invocation1, memberAccess1, (LambdaExpressionSyntax)expression1);
                }
            }
        }

        private static void FadeOut(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess, LambdaExpressionSyntax lambda)
        {
            TextSpan span = TextSpan.FromBounds(memberAccess.OperatorToken.Span.Start, lambda.ArrowToken.Span.End);

            context.ReportDiagnostic(FadeOutDescriptor, Location.Create(invocation.SyntaxTree, span));
            context.ReportDiagnostic(FadeOutDescriptor, invocation.ArgumentList.CloseParenToken.GetLocation());
        }

        private static bool CheckLambdas(ExpressionSyntax expression1, ExpressionSyntax expression2)
        {
            switch (expression1.Kind())
            {
                case SyntaxKind.SimpleLambdaExpression:
                    {
                        if (expression2?.IsKind(SyntaxKind.SimpleLambdaExpression) == true)
                        {
                            var lambda1 = (SimpleLambdaExpressionSyntax)expression1;
                            var lambda2 = (SimpleLambdaExpressionSyntax)expression2;

                            return ParameterIdentifierEquals(lambda1.Parameter, lambda2.Parameter)
                                && lambda1.Body is ExpressionSyntax
                                && lambda1.Body is ExpressionSyntax;
                        }

                        break;
                    }
                case SyntaxKind.ParenthesizedLambdaExpression:
                    {
                        if (expression2?.IsKind(SyntaxKind.ParenthesizedLambdaExpression) == true)
                        {
                            var lambda1 = (ParenthesizedLambdaExpressionSyntax)expression1;
                            var lambda2 = (ParenthesizedLambdaExpressionSyntax)expression2;

                            if (lambda1 is ExpressionSyntax
                                && lambda2 is ExpressionSyntax)
                            {
                                ParameterListSyntax parameterList1 = lambda1.ParameterList;
                                ParameterListSyntax parameterList2 = lambda2.ParameterList;

                                if (parameterList1 != null
                                    && parameterList2 != null)
                                {
                                    SeparatedSyntaxList<ParameterSyntax> parameters1 = parameterList1.Parameters;
                                    SeparatedSyntaxList<ParameterSyntax> parameters2 = parameterList2.Parameters;

                                    if (parameters1.Count == parameters2.Count)
                                    {
                                        for (int i = 0; i < parameters1.Count; i++)
                                        {
                                            if (!ParameterIdentifierEquals(parameters1[i], parameters2[i]))
                                                return false;
                                        }

                                        return true;
                                    }
                                }
                            }
                        }

                        break;
                    }
            }

            return false;
        }

        private static bool ParameterIdentifierEquals(ParameterSyntax parameter1, ParameterSyntax parameter2)
        {
            return parameter1?.Identifier.ValueText.Equals(parameter2.Identifier.ValueText, StringComparison.Ordinal) == true;
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

            var invocation2 = (InvocationExpressionSyntax)memberAccess.Expression;

            ExpressionSyntax expression1 = GetCondition(invocation);
            ExpressionSyntax expression2 = GetCondition(invocation2);

            InvocationExpressionSyntax newInvocation = invocation2.ReplaceNode(
                expression2,
                LogicalAndExpression(expression2, expression1, addParenthesesIfNecessary: true));

            newInvocation = newInvocation.WithFormatterAnnotation();

            return await document.ReplaceNodeAsync(invocation, newInvocation, cancellationToken).ConfigureAwait(false);
        }

        private static ExpressionSyntax GetCondition(InvocationExpressionSyntax invocation)
        {
            ArgumentSyntax argument = invocation.ArgumentList.Arguments.First();

            var lambda = (LambdaExpressionSyntax)argument.Expression;

            return (ExpressionSyntax)lambda.Body;
        }
    }
}
