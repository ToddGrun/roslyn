// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Benchmarks;

[MemoryDiagnoser]
public class StringSwitchBenchmarks_RoslynSolutionEmit
{
    private List<Compilation>? _solutionCompilations = null!;

    [GlobalSetup]
    public void Setup()
    {
        // prepare solution compilations for EmitAllSolutionProjects benchmark
        PrepareSolutionCompilations();
    }

    [Benchmark]
    public void EmitAllSolutionProjects()
    {
        if (_solutionCompilations == null)
        {
            throw new InvalidOperationException("Solution compilations were not prepared in Setup.");
        }

        foreach (var comp in _solutionCompilations)
        {
            // emit into a null sink to avoid retaining large byte arrays
            using var ns = new NullStream();
            var result = comp.Emit(ns);
            if (!result.Success)
            {
                throw new Exception("Emit failed for project: " + comp.AssemblyName + " - " + string.Join(";", result.Diagnostics));
            }
        }
    }

    // Prepare solution compilations in setup so the Emit-only benchmark can just iterate and emit.
    private void PrepareSolutionCompilations()
    {
        // find solution
        var solutionPath = FindSolutionPath("Roslyn.slnx");
        if (solutionPath == null)
        {
            // no solution found from current directory; skip preparing solution compilations
            _solutionCompilations = null;
            return;
        }

        var solutionDir = Path.GetDirectoryName(solutionPath)!;

        // register MSBuild if available
        if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
        {
            var instances = Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances();
            var instance = instances.OrderByDescending(i => i.Version).FirstOrDefault();
            if (instance != null)
            {
                Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);
            }
        }

        var props = new Dictionary<string, string>
        {
            ["Configuration"] = "Release"
        };

        using var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create(props);
        var solution = workspace.OpenSolutionAsync(solutionPath).GetAwaiter().GetResult();
        var comps = new List<Compilation>();
        var compilersDir = Path.Combine(solutionDir, "src", "Compilers");
        foreach (var project in solution.Projects)
        {
            // only compile projects under src/Compilers to limit scope
            var filePath = project.FilePath ?? string.Empty;
            if (!filePath.StartsWith(compilersDir, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (project.Language != LanguageNames.CSharp)
            {
                continue;
            }

            var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
            if (compilation != null)
            {
                comps.Add(compilation);
            }
        }

        _solutionCompilations = comps;
    }

    private static string? FindSolutionPath(string solutionFileName)
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, solutionFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        return null;
    }

    private sealed class NullStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) { /* discard */ }
    }
}


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
