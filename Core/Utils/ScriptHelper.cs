using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;

namespace Sharplings.Utils;

static class ScriptHelper {
    public static async Task<EmitResult> EmitCompilationAsync(string scriptPath, Stream peStream, Stream? pdbStream) {
        string scriptContent = await File.ReadAllTextAsync(scriptPath);

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(scriptContent, encoding: Encoding.Default, path: scriptPath);
        CSharpCompilation compilation = CSharpCompilation.Create(Path.GetFileName(scriptPath))
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(GetDefaultMetadataReferences());

        return compilation.Emit(peStream, pdbStream);
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
