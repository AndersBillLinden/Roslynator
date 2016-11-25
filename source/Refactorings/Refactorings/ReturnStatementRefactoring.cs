﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ReturnStatementRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, ReturnStatementSyntax returnStatement)
        {
            ExpressionSyntax expression = returnStatement.Expression;

            if (expression != null)
            {
                if (context.IsAnyRefactoringEnabled(
                    RefactoringIdentifiers.AddBooleanComparison,
                    RefactoringIdentifiers.ChangeMemberTypeAccordingToReturnExpression,
                    RefactoringIdentifiers.AddCastExpression,
                    RefactoringIdentifiers.CallToMethod))
                {
                    await ReturnExpressionRefactoring.ComputeRefactoringsAsync(context, expression).ConfigureAwait(false);
                }

                if (context.IsRefactoringEnabled(RefactoringIdentifiers.CreateConditionFromBooleanExpression))
                    await CreateConditionFromBooleanExpressionRefactoring.ComputeRefactoringAsync(context, expression).ConfigureAwait(false);
            }
            else if (context.IsRefactoringEnabled(RefactoringIdentifiers.AddDefaultValueToReturnStatement))
            {
                await AddDefaultValueToReturnStatementRefactoring.ComputeRefactoringsAsync(context, returnStatement).ConfigureAwait(false);
            }
        }
    }
}
