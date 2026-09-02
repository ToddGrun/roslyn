// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Benchmarks;

[MemoryDiagnoser]
public class StringSwitchBenchmarks
{
    [Params(3, 6, 8, 20, 100)]
    public int CaseCount { get; set; }

    private CSharpCompilation _comp = null!;
    private ImmutableArray<(ConstantValue value, LabelSymbol label)> _inputArray;

    [GlobalSetup]
    public void Setup()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var source = GenerateSource(CaseCount);

        // basic reference set: mscorlib / System.Private.CoreLib etc.
        var core = typeof(object).GetTypeInfo().Assembly.Location;
        var systemConsole = typeof(Console).GetTypeInfo().Assembly.Location;
        var refs = new[]
        {
            MetadataReference.CreateFromFile(core),
            MetadataReference.CreateFromFile(systemConsole)
        };

        var compOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithOptimizationLevel(OptimizationLevel.Release);

        // Pre-create syntax tree and compilation so we can benchmark Emit-only separately.
        var sourceText = SourceText.From(source);
        var tree = CSharpSyntaxTree.ParseText(sourceText, parseOptions);
        _comp = CSharpCompilation.Create("Bench", [tree], refs, compOptions);

        // build input ImmutableArray<(ConstantValue, LabelSymbol)> for Call_LengthBasedStringSwitchData_Create benchmark
        var builder = ArrayBuilder<(ConstantValue value, LabelSymbol label)>.GetInstance();
        for (var i = 0; i < CaseCount; i++)
        {
            var constValue = ConstantValue.Create(GenerateStringForIndex(i));
            var label = new GeneratedLabelSymbol("lbl" + i);
            builder.Add((constValue, label));
        }

        _inputArray = builder.ToImmutableAndFree();
    }

    private static string GenerateSource(int n)
    {
        var sb = new StringBuilder();
        sb.AppendLine("public static class C {");
        sb.AppendLine("  public static string M(string s) {");
        sb.Append("    return s switch { ");

        for (var i = 0; i < n; i++)
        {
            var v = GenerateStringForIndex(i);
            sb.Append($"\"{v}\" => \"{v}\", ");
        }

        sb.AppendLine("_ => \"default\" }; ");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateStringForIndex(int i)
    {
        return (i % 4) switch
        {
            0 => "a" + i,
            1 => "b" + i,
            2 => new string('x', 3 + i % 5) + i,
            _ => new string('y', 6 + i % 7) + i
        };
    }

    [Benchmark]
    public void EmitOnly()
    {
        using var ms = new MemoryStream();
        var result = _comp.Emit(ms);

        GC.KeepAlive(result);
    }

    [Benchmark]
    public void Call_LengthBasedStringSwitchData_Create()
    {
        _ = LengthBasedStringSwitchData.TryCreate(_inputArray, out var result);

        GC.KeepAlive(result);
    }
}
