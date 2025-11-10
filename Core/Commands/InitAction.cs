using System.CommandLine;
using System.CommandLine.Invocation;
using Sharplings.Utils;
using Spectre.Console;

namespace Sharplings.Commands;

class InitAction : AsynchronousCommandLineAction {
    const string InitSolutionFileTemplate =
        """
        class {0} {{
            public static void Main() {{
                // DON'T EDIT THIS SOLUTION FILE!
                // It will be automatically filled after you finish the exercise.
            }}
        }}
        """;

    const string Gitignore =
        """
        bin/
        obj/
        .vscode/
        .idea/
        """;

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default) {
        DirectoryInfo dirInfo = parseResult.GetValue<DirectoryInfo>("--output") ?? new DirectoryInfo("Sharplings");

        if (dirInfo.Exists) {
            throw new InvalidOperationException($"""
            A directory with the name `{dirInfo.Name}` already exists in the current directory.
            You probably already initialized Sharplings.
            Run `cd {dirInfo.Name}`
            Then run `Sharplings` again
            """);
        }

        if (Directory.Exists("Exercises") && Directory.Exists("Solutions")) {
            throw new InvalidOperationException("""
            It looks like Sharplings is already initialized in this directory.
            
            If you already initialized Sharplings, run the command `Sharplings` for instructions on getting started with the exercises.
            Otherwise, please run `Sharplings init` again in a different directory.
            """);
        }

        if (Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.sln").Any() ||
            Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.csproj").Any()) {
            throw new InvalidOperationException("""
            The current directory is already part of a C# project.
            Please initialize Sharplings in a different directory.
            """);
        }

        AnsiConsole.WriteLine($"""
        This command will create the directory `{dirInfo.Name}/` which will contain the exercises.
        Press ENTER to continue
        """);
        Console.ReadLine();
        AnsiConsole.Clear();

        dirInfo.Create();
        Directory.SetCurrentDirectory(dirInfo.FullName);

        EmbeddedFiles embeddedFiles = EmbeddedFilesFactory.Instance;

        InfoFile infoFile = await InfoFile.ParseAsync(cancellationToken);
        await embeddedFiles.InitExercisesDirAsync(infoFile.Exercises, cancellationToken);

        Directory.CreateDirectory("Solutions");
        await File.WriteAllBytesAsync("Solutions/README.md", embeddedFiles.Files["SolutionsReadme"], cancellationToken);

        foreach (ExerciseDir dir in embeddedFiles.ExerciseDirs) {
            string dirPath = Path.Combine("Solutions", dir.Name);
            Directory.CreateDirectory(dirPath);
        }

        foreach (ExerciseInfo exerciseInfo in infoFile.Exercises) {
            string solutionPath = exerciseInfo.SolutionPath;
            await File.WriteAllTextAsync(solutionPath, string.Format(InitSolutionFileTemplate, exerciseInfo.Name), cancellationToken);
        }

        await File.WriteAllTextAsync(".gitignore", Gitignore, cancellationToken);
        await File.WriteAllBytesAsync(".editorconfig", embeddedFiles.Files["EditorConfig"], cancellationToken);
        await File.WriteAllBytesAsync("Sharplings.sln", embeddedFiles.Files["SharplingsSln"], cancellationToken);
        // todo vscode extensions

        string exercisesCsprojPath = Path.Combine("Exercises", "Exercises.csproj");
        await File.WriteAllBytesAsync(exercisesCsprojPath, embeddedFiles.Files["ExercisesCsproj"], cancellationToken);

        string solutionsCsprojPath = Path.Combine("Solutions", "Solutions.csproj");
        await File.WriteAllBytesAsync(solutionsCsprojPath, embeddedFiles.Files["SolutionsCsproj"], cancellationToken);

        AnsiConsole.MarkupLine("[lime]Initialization done[/]");
        AnsiConsole.MarkupLine($"[bold]Run `cd {dirInfo.Name}` to go into the generated directory.\n" +
                               "Then run `Sharplings` to get started.[/]");

        return 0;
    }
}
