using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Sharplings.Terminal;

namespace Sharplings;

/// See <see cref="ExerciseInfo"/>
class Exercise : RunnableExercise {
    public required string? FullPath { get; init; }
    public required string Hint { get; init; }
    public required bool Done { get; set; }
}

abstract class RunnableExercise {
    public required string? Directory { get; init; }
    public required string Name { get; init; }
    public required bool Test { get; init; }
    public required bool StrictAnalyzer { get; init; }

    public string Path => GetPath("Exercises");
    public string SolutionPath => GetPath("Solutions");

    async Task<bool> Run(string path, bool forceStrictAnalyzer, TerminalOutputData outputData) {
        outputData.CompilationOutput = "";
        outputData.ExerciseOutput = "";

        string scriptContent = await File.ReadAllTextAsync(path);

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(scriptContent);
        CSharpCompilation compilation = CSharpCompilation.Create(System.IO.Path.GetFileName(path))
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(GetDefaultMetadataReferences());

        await using MemoryStream memoryStream = new();
        EmitResult emitResult = compilation.Emit(memoryStream);

        outputData.CompilationOutput = string.Join(Environment.NewLine, emitResult.Diagnostics);
        if (!emitResult.Success) return false;

        // todo maybe use `diagnostic.DefaultSeverity`
        if (emitResult.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            return false;

        // todo tests

        if (StrictAnalyzer || forceStrictAnalyzer) {
            if (emitResult.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning))
                return false;
        }

        Assembly assembly = Assembly.Load(memoryStream.ToArray());

        if (assembly.EntryPoint == null)
            throw new EntryPointNotFoundException($"Script {path} should have an entry point");

        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;

        StringWriter redirectedWriter = new();

        try {
            Console.SetOut(redirectedWriter);
            Console.SetError(redirectedWriter);

            assembly.EntryPoint.Invoke(null, null);
            outputData.ExerciseOutput = redirectedWriter.ToString();
            return true;
        } catch (Exception e) {
            outputData.ExerciseOutput = e.ToString();
            return false;
        } finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    public Task<bool> RunExercise(TerminalOutputData outputData) =>
        Run(Path, false, outputData);

    public Task<bool> RunSolution(TerminalOutputData outputData) =>
        Run(SolutionPath, true, outputData);

    protected string GetPath(string from) {
        string path = !string.IsNullOrWhiteSpace(Directory)
            ? System.IO.Path.Combine(from, Directory)
            : from;

        return System.IO.Path.Combine(path, $"{Name}.cs");
    }

    static IEnumerable<MetadataReference> GetDefaultMetadataReferences() {
        ScriptMetadataResolver metadataResolver = ScriptMetadataResolver.Default;
        HashSet<MetadataReference> defaultReferences = [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];

        foreach (MetadataReference reference in ScriptOptions.Default.MetadataReferences) {
            if (reference is UnresolvedMetadataReference unresolved) {
                ImmutableArray<PortableExecutableReference> resolved =
                    metadataResolver.ResolveReference(unresolved.Reference, null, unresolved.Properties);

                if (resolved.IsDefault)
                    throw new InvalidOperationException($"Cannot resolve reference {unresolved.Reference}");

                foreach (PortableExecutableReference executableReference in resolved) {
                    defaultReferences.Add(executableReference);
                }
            } else {
                defaultReferences.Add(reference);
            }
        }

        return defaultReferences.ToImmutableHashSet();
    }
}
