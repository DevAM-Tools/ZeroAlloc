// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// System
global using System.Collections.Immutable;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Threading;

// Microsoft
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Diagnostics;
global using Microsoft.CodeAnalysis.Emit;

// Third-party
global using TUnit.Assertions;
global using TUnit.Assertions.Extensions;
global using TUnit.Core;

// Internal
global using ZeroAlloc.Generator;
global using ZeroAlloc.Generator.Tests.Helpers;
