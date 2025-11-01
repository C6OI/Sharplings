using System.Text;
using JetBrains.Annotations;
using Tomlyn;

namespace Sharplings;

[PublicAPI]
public class ExerciseInfo {
    public string Name { get; private set; } = "";
    public string? Directory { get; private set; }
    public bool Test { get; private set; } = true;
    public bool StrictAnalyzer { get; private set; }
    public string Hint { get; private set; } = "";
    public bool SkipCheckUnsolved { get; private set; }

    public string SolutionPath {
        get {
            string path = string.IsNullOrWhiteSpace(Directory)
                ? "Solutions"
                : Path.Combine("Solutions", Directory);

            return Path.Combine(path, $"{Name}.cs");
        }
    }
}

[PublicAPI]
public class InfoFile {
    public int FormatVersion { get; private set; }
    public string WelcomeMessage { get; private set; } = "";
    public string FinalMessage { get; private set; } = "";
    public IList<ExerciseInfo> Exercises { get; private set; } = [];

    public static async Task<InfoFile> ParseAsync(CancellationToken cancellationToken = default) {
        string content;

        if (File.Exists("info.toml")) {
            content = await File.ReadAllTextAsync("info.toml", cancellationToken);
        } else {
            content = Encoding.Default.GetString(EmbeddedFilesFactory.Instance.Files["InfoFile"]);
        }

        InfoFile infoFile = Toml.ToModel<InfoFile>(content);

        if (infoFile.Exercises.Count == 0) {
            throw new InvalidOperationException("""
                                                    There are no exercises yet!
                                                    Add at least one exercise before testing.
                                                """);
        }

        return infoFile;
    }
}
