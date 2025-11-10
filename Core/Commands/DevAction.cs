using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Sharplings.Commands;

partial class DevAction(
    DevCommand command
) : AsynchronousCommandLineAction {
    const string Readme =
        """
        # Sharplings #️⃣
        
        Welcome to these community Sharplings exercises 😃
        
        First, [install Sharplings using the official instructions](https://github.com/C6OI/Sharplings) ✅
        
        Then, clone this repository, open a terminal in this directory and run `Sharplings` to get started with the exercises 🚀
        """;

    const string Gitignore =
        """
        bin/
        obj/
        .vscode/
        .idea/
        """;

    const string InfoFileBeforeFormatVersion =
        """
        # The format version is an indicator of the compatibility of community exercises with the
        # Sharplings program.
        # The format version is not the same as the version of the Sharplings program.
        # In case Sharplings makes an unavoidable breaking change to the expected format of community
        # exercises, you would need to raise this version and adapt to the new format.
        # Otherwise, the newest version of the Sharplings program won't be able to run these exercises.
        format_version = 
        """;

    const string InfoFileAfterFormatVersion =
        """"
        
        
        # Optional multi-line message to be shown to users when just starting with the exercises.
        welcome_message = """Welcome to these community Sharplings exercises."""
        
        # Optional multi-line message to be shown to users after finishing all exercises.
        final_message = """We hope that you found the exercises helpful :D"""
        
        # Repeat this section for every exercise.
        [[exercises]]
        # Exercise name which is the exercise file name without the `.cs` extension.
        name = "???"
        
        # Optional directory name to be provided if you want to organize exercises in directories.
        # If `dir` is specified, the exercise path is `Exercises/DIR/NAME.cs`
        # Otherwise, the path is `Exercises/NAME.cs`
        # dir = "???"
        
        # Sharplings expects the exercise to contain tests and run them.
        # You can optionally disable testing by setting `test` to `false` (the default is `true`).
        # In that case, the exercise will be considered done when it just successfully compiles.
        # test = true
        
        # You can optionally set `strict_analyzer` to `true` (the default is `false`) to only consider
        # the exercise as done when there are no warnings left.
        # strict_analyzer = false
        
        # A multi-line hint to be shown to users on request.
        hint = """???"""
        """";

    const string SkipCheckUnsolvedHint =
        "If this is an introduction exercise that is intended to be already solved, add `skip_check_unsolved = true` to the exercise's metadata in the `info.toml` file";

    public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default) => command switch {
        DevCommand.New => New(parseResult.GetRequiredValue<DirectoryInfo>("path"), parseResult.GetRequiredValue<bool>("--no_git"), cancellationToken),
        DevCommand.Check => Check(parseResult.GetRequiredValue<bool>("--require_solutions"), cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
    };

    static async Task<int> New(DirectoryInfo path, bool noGit, CancellationToken cancellationToken) {
        //Debug.Fail("Disabled in the debug build");

        try {
            return await NewImpl(path, noGit, cancellationToken);
        } catch (Exception e) {
            throw new AggregateException("""
            
            Initialization failed.
            After resolving the issue, delete the `Sharplings` directory (if it was created) and try again
            
            """, e);
        }
    }

    static async Task<int> Check(bool requireSolutions, CancellationToken cancellationToken) {
        const int maxExercisesCount = 999;

        InfoFile infoFile = await InfoFile.ParseAsync(cancellationToken);

        if (infoFile.Exercises.Count > maxExercisesCount) {
            throw new InvalidOperationException($"The maximum number of exercises is {maxExercisesCount}");
        }

        await CheckExercises(infoFile, cancellationToken);
        await CheckSolutions(requireSolutions, infoFile, cancellationToken);

        AnsiConsole.MarkupLine("[lime]Everything looks fine![/]");
        return 0;
    }

    static async Task<int> NewImpl(DirectoryInfo path, bool noGit, CancellationToken cancellationToken) {
        string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), path.FullName);

        path.Create();
        AnsiConsole.WriteLine($"Created the directory {relativePath}");

        Directory.SetCurrentDirectory(path.FullName);

        if (!noGit) {
            Process gitInit = new() {
                StartInfo = new ProcessStartInfo {
                    FileName = "git",
                    Arguments = "init",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            gitInit.OutputDataReceived += (_, e) => Console.Out.WriteLine(e.Data);
            gitInit.ErrorDataReceived += (_, e) => Console.Error.WriteLine(e.Data);

            gitInit.Start();

            gitInit.BeginOutputReadLine();
            gitInit.BeginErrorReadLine();
            await gitInit.WaitForExitAsync(cancellationToken);

            if (gitInit.ExitCode != 0) {
                throw new InvalidOperationException("`git init` didn't run successfully. See the possible error message above");
            }
        }

        EmbeddedFiles embeddedFiles = EmbeddedFilesFactory.Instance;

        CreateDirectoryRel("Exercises", relativePath);
        CreateDirectoryRel("Solutions", relativePath);

        await WriteFileRelAsync($"{path.Name}.sln", relativePath, embeddedFiles.Files["SharplingsSln"], cancellationToken);
        await WriteFileRelAsync(Path.Combine("Exercises", "Exercises.csproj"), relativePath, embeddedFiles.Files["ExercisesCsproj"], cancellationToken);
        await WriteFileRelAsync(Path.Combine("Solutions", "Solutions.csproj"), relativePath, embeddedFiles.Files["SolutionsCsproj"], cancellationToken);

        await WriteFileRelAsync(".editorconfig", relativePath, embeddedFiles.Files["EditorConfig"], cancellationToken);
        await WriteFileRelAsync(".gitignore", relativePath, Gitignore, cancellationToken);
        await WriteFileRelAsync("info.toml", relativePath,
            InfoFileBeforeFormatVersion + InfoFile.CurrentFormatVersion + InfoFileAfterFormatVersion,
            cancellationToken);

        await WriteFileRelAsync("README.md", relativePath, Readme, cancellationToken);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[lime]Initialization done[/]");
        return 0;
    }

    static async Task WriteFileRelAsync(string fileName, string relativeTo, string content, CancellationToken cancellationToken) {
        await File.WriteAllTextAsync(fileName, content, cancellationToken);
        AnsiConsole.WriteLine($"Created the file {Path.Combine(relativeTo, fileName)}");
    }

    static async Task WriteFileRelAsync(string fileName, string relativeTo, byte[] content, CancellationToken cancellationToken) {
        await File.WriteAllBytesAsync(fileName, content, cancellationToken);
        AnsiConsole.WriteLine($"Created the file {Path.Combine(relativeTo, fileName)}");
    }

    static void CreateDirectoryRel(string dirName, string relativeTo) {
        Directory.CreateDirectory(dirName);
        AnsiConsole.WriteLine($"Created the directory {Path.Combine(relativeTo, dirName)}");
    }

    static async Task CheckExercises(InfoFile infoFile, CancellationToken cancellationToken) {
        if (infoFile.FormatVersion > InfoFile.CurrentFormatVersion) {
            throw new InvalidOperationException($"""
             `format_version` < {InfoFile.CurrentFormatVersion} (supported version)
             Please migrate to the latest format version
             """);
        }

        if (infoFile.FormatVersion < InfoFile.CurrentFormatVersion) {
            throw new InvalidOperationException($"""
             `format_version` > {InfoFile.CurrentFormatVersion} (supported version)
             Try updating the Rustlings program
             """);
        }

        await CheckExercisesUnsolved(infoFile);
        HashSet<string> paths = await CheckInfoFileExercises(infoFile, cancellationToken);
        CheckUnexpectedFiles(new DirectoryInfo("Exercises"), paths);
    }

    static async Task CheckSolutions(bool requireSolutions, InfoFile infoFile, CancellationToken cancellationToken) {
        AnsiConsole.WriteLine("Running all solutions...");

        IEnumerable<Task<ISolutionCheck>> tasks = infoFile.Exercises
            .Select(exerciseInfo => Task.Run<ISolutionCheck>(async () => {
                string solutionPath = exerciseInfo.SolutionPath;
                if (!File.Exists(solutionPath)) {
                    return requireSolutions
                        ? throw new InvalidOperationException($"The solution of the exercise {exerciseInfo.Name} is missing")
                        : new MissingOptional();
                }

                StringBuilder output = new(1 << 14);
                bool success = await exerciseInfo.RunSolution(output);

                return success
                    ? new Success(solutionPath)
                    : new RunFailure(output.ToString());
            }, cancellationToken));

        HashSet<string> solutionPaths = new(infoFile.Exercises.Count);

        await AnsiConsole
            .Progress()
            .AutoRefresh(true)
            .StartAsync(async ctx => {
                ProgressTask progressTask = ctx.AddTask("Running solutions", maxValue: infoFile.Exercises.Count);

                foreach ((ExerciseInfo exerciseInfo, Task<ISolutionCheck> task) in infoFile.Exercises.Zip(tasks)) {
                    ISolutionCheck result = await task;

                    switch (result) {
                        case MissingOptional: break;

                        case RunFailure(var output): {
                            AnsiConsole.Markup(output);
                            throw new InvalidOperationException(
                                $"Running the solution of the exercise {exerciseInfo.Name} failed with the error above");
                        }

                        case Success(var solutionPath): {
                            solutionPaths.Add(solutionPath);
                            break;
                        }

                        default: throw new ArgumentOutOfRangeException(nameof(result));
                    }

                    progressTask.Increment(1);
                }
            });

        CheckUnexpectedFiles(new DirectoryInfo("Solutions"), solutionPaths);
    }

    static async Task CheckExercisesUnsolved(InfoFile infoFile) {
        AnsiConsole.WriteLine("Running all exercises to check that they aren't already solved...");

        IEnumerable<Task<bool>> tasks = infoFile.Exercises
            .Select(exerciseInfo => exerciseInfo.SkipCheckUnsolved
                ? Task.FromResult(false)
                : exerciseInfo.RunExercise(null));

        await AnsiConsole
            .Progress()
            .AutoRefresh(true)
            .StartAsync(async ctx => {
                ProgressTask progressTask = ctx.AddTask("Ensuring exercises unsolved", maxValue: infoFile.Exercises.Count);

                foreach ((ExerciseInfo exerciseInfo, Task<bool> task) in infoFile.Exercises.Zip(tasks)) {
                    if (exerciseInfo.SkipCheckUnsolved) {
                        progressTask.Increment(1);
                        continue;
                    }

                    bool success = await task;

                    if (success) {
                        throw new InvalidOperationException($"""
                        The exercise {exerciseInfo.Name} is already solved.
                        {SkipCheckUnsolvedHint}
                        """);
                    }

                    progressTask.Increment(1);
                }
            });
    }

    /// Check the info of all exercises and return their paths in a set.
    static async Task<HashSet<string>> CheckInfoFileExercises(InfoFile infoFile, CancellationToken cancellationToken) {
        const int maxExerciseNameLength = 32;

        HashSet<string> names = new(infoFile.Exercises.Count);
        HashSet<string> paths = new(infoFile.Exercises.Count);

        foreach (ExerciseInfo exerciseInfo in infoFile.Exercises) {
            string name = exerciseInfo.Name;

            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, maxExerciseNameLength);

            List<char> forbiddenNameChars = ForbiddenChars(name).ToList();
            if (forbiddenNameChars.Count != 0) {
                throw new ArgumentException(
                    $"Chars `{string.Join(", ", forbiddenNameChars)}` in the exercise name `{name}` aren't allowed", nameof(name));
            }

            string? directory = exerciseInfo.Directory;
            if (directory != null) {
                ArgumentException.ThrowIfNullOrWhiteSpace(directory);

                List<char> forbiddenDirChars = ForbiddenChars(directory).ToList();
                if (forbiddenDirChars.Count != 0) {
                    throw new ArgumentException(
                        $"Chars `{string.Join(", ", forbiddenDirChars)}` in the exercise directory `{directory}` aren't allowed", nameof(directory));
                }
            }

            if (string.IsNullOrWhiteSpace(exerciseInfo.Hint)) {
                throw new ArgumentException(
                    $"The exercise `{name}` has an empty hint. Please provide a hint or at least tell the user why a hint isn't needed for this exercise",
                    nameof(name));
            }

            if (!names.Add(name)) {
                throw new ArgumentException($"The exercise name `{name}` is duplicated. Exercise names must all be unique");
            }

            string path = exerciseInfo.Path;
            string exerciseContent = await File.ReadAllTextAsync(path, cancellationToken);

            if (!EntryPointRegex().IsMatch(exerciseContent)) {
                throw new ArgumentException($"""
                Entry point is missing in the file `{path}`.
                Create at least an empty `public static void Main()` method
                """);
            }

            if (!exerciseContent.Contains("// TODO")) {
                throw new ArgumentException($"""
                Didn't find any `// TODO` comment in the file `{path}`.
                You need to have at least one such comment to guide the user.
                """);
            }

            if (exerciseInfo.Test)
                throw new NotImplementedException("Tests are not yet implemented");

            paths.Add(path);
        }

        return paths;
    }

    /// Check `<paramref name="dir"/>` for unexpected files.<br/>
    /// Only C# files in `<paramref name="allowedCsFiles"/>`, `README.md` and `*.csproj` files are allowed.<br/>
    /// Only one level of directory nesting is allowed.
    static void CheckUnexpectedFiles(DirectoryInfo dir, ISet<string> allowedCsFiles) {
        foreach (FileSystemInfo entry in dir.EnumerateFileSystemInfos()) {
            if (entry is FileInfo file) {
                string path = dir.Parent != null ? Path.GetRelativePath(dir.Parent.FullName, file.FullName) : file.FullName;
                string name = file.Name;

                if (name == "README.md" || Path.GetExtension(name) == ".csproj")
                    continue;

                if (!allowedCsFiles.Contains(path))
                    UnexpectedFile(dir, path);

                continue;
            }

            DirectoryInfo dirEntry = (DirectoryInfo)entry;
            foreach (FileSystemInfo innerEntry in dirEntry.EnumerateFileSystemInfos()) {
                string path = dir.Parent != null ? Path.GetRelativePath(dir.Parent.FullName, innerEntry.FullName) : innerEntry.FullName;
                if (innerEntry is not FileInfo innerFile)
                    throw new InvalidOperationException($"Found {path} but expected only files. Only one level of exercise nesting is allowed");

                string name = innerFile.Name;
                if (name == "README.md")
                    continue;

                if (!allowedCsFiles.Contains(path))
                    UnexpectedFile(dir, path);
            }
        }
        return;

        [DoesNotReturn]
        static void UnexpectedFile(DirectoryInfo dir, string path) =>
            throw new InvalidOperationException(
                $"Found the file `{path}`. Only `README.md` and C# files related to an exercise in `info.toml` are allowed in the `{dir}` directory");
    }

    /// Find a chars that aren't allowed in the exercise's `Name` or `Directory`.
    static IEnumerable<char> ForbiddenChars(string input) =>
        input.Where(c => !char.IsLetterOrDigit(c) && c != '_');

    [GeneratedRegex(
        @"^\s*public static (((async )?(System\.Threading\.Tasks\.)?Task(\<int\>)?)|void|int) Main\((string\[\] [A-Za-z_@][A-Za-z0-9_@]*)?\)",
        RegexOptions.Multiline)]
    private static partial Regex EntryPointRegex();
}

enum DevCommand {
    New,
    Check
}

interface ISolutionCheck;

record struct Success(string SolutionPath) : ISolutionCheck;

struct MissingOptional : ISolutionCheck;

record struct RunFailure(string Output) : ISolutionCheck;
