﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class DeclareEachAttributeSeparatelyRefactoring
    {
        public static bool CanRefactor(AttributeListSyntax attributeList)
        {
            return attributeList.Attributes.Count > 1;
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            AttributeListSyntax attributeList,
            CancellationToken cancellationToken)
        {
            return await document.ReplaceNodeAsync(
                attributeList,
                AttributeRefactoring.SplitAttributes(attributeList).Select(f => f.WithFormatterAnnotation())).ConfigureAwait(false);
        }
    }
}
