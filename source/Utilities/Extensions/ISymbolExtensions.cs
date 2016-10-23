﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Pihrtsoft.CodeAnalysis
{
    public static class ISymbolExtensions
    {
        public static bool TryGetConstantValue(this ISymbol symbol, out object constantValue)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    {
                        var field = (IFieldSymbol)symbol;

                        if (field.HasConstantValue)
                        {
                            constantValue = field.ConstantValue;
                            return true;
                        }

                        break;
                    }
                case SymbolKind.Local:
                    {
                        var local = (ILocalSymbol)symbol;

                        if (local.HasConstantValue)
                        {
                            constantValue = local.ConstantValue;
                            return true;
                        }

                        break;
                    }
            }

            constantValue = null;
            return false;
        }

        public static bool IsKind(this ISymbol symbol, SymbolKind kind)
        {
            return symbol?.Kind == kind;
        }

        public static bool IsKind(this ISymbol symbol, SymbolKind kind1, SymbolKind kind2)
        {
            if (symbol == null)
                return false;

            SymbolKind kind = symbol.Kind;

            return kind == kind1
                || kind == kind2;
        }

        public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).Parameters;
                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).Parameters;
                default:
                    return ImmutableArray<IParameterSymbol>.Empty;
            }
        }

        public static bool IsGenericIEnumerable(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            return symbol?.IsNamedType() == true
                && ((INamedTypeSymbol)symbol).ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
        }

        public static bool IsGenericImmutableArray(this ISymbol symbol, SemanticModel semanticModel)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (semanticModel == null)
                throw new ArgumentNullException(nameof(semanticModel));

            if (symbol?.IsNamedType() == true)
            {
                INamedTypeSymbol namedTypeSymbol = semanticModel
                    .Compilation
                    .GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");

                return namedTypeSymbol != null
                    && ((INamedTypeSymbol)symbol).ConstructedFrom.Equals(namedTypeSymbol);
            }

            return false;
        }

        [DebuggerStepThrough]
        public static bool IsPublic(this ISymbol symbol)
        {
            return symbol?.DeclaredAccessibility == Accessibility.Public;
        }

        [DebuggerStepThrough]
        public static bool IsInternal(this ISymbol symbol)
        {
            return symbol?.DeclaredAccessibility == Accessibility.Internal;
        }

        [DebuggerStepThrough]
        public static bool IsProtected(this ISymbol symbol)
        {
            return symbol?.DeclaredAccessibility == Accessibility.Protected;
        }

        [DebuggerStepThrough]
        public static bool IsPrivate(this ISymbol symbol)
        {
            return symbol?.DeclaredAccessibility == Accessibility.Private;
        }

        [DebuggerStepThrough]
        public static bool IsArrayType(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.ArrayType;
        }

        [DebuggerStepThrough]
        public static bool IsDynamicType(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.DynamicType;
        }

        [DebuggerStepThrough]
        public static bool IsErrorType(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.ErrorType;
        }

        [DebuggerStepThrough]
        public static bool IsEvent(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Event;
        }

        [DebuggerStepThrough]
        public static bool IsField(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Field;
        }

        [DebuggerStepThrough]
        public static bool IsEnumField(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Field
                && symbol.ContainingType?.TypeKind == TypeKind.Enum;
        }

        [DebuggerStepThrough]
        public static bool IsLocal(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Local;
        }

        [DebuggerStepThrough]
        public static bool IsMethod(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Method;
        }

        [DebuggerStepThrough]
        public static bool IsAsyncMethod(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Method
                && ((IMethodSymbol)symbol).IsAsync;
        }

        [DebuggerStepThrough]
        public static bool IsNamedType(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.NamedType;
        }

        [DebuggerStepThrough]
        public static bool IsNamespace(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Namespace;
        }

        [DebuggerStepThrough]
        public static bool IsParameter(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Parameter;
        }

        [DebuggerStepThrough]
        public static bool IsProperty(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Property;
        }

        [DebuggerStepThrough]
        public static bool IsTypeParameter(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.TypeParameter;
        }
    }
}
