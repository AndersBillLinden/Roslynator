﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Roslynator.CSharp
{
    public enum TypeAnalysisResult
    {
        None,
        Implicit,
        ImplicitButShouldBeExplicit,
        Explicit,
        ExplicitButShouldBeImplicit
    }
}
