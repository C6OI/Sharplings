using System.CommandLine;
using Sharplings;
using Sharplings.Commands;
using Sharplings.Terminal;
using Spectre.Console;

const int currentFormatVersion = 1;

ParseResult parseResult = GenerateCommands().Parse(args);

if (parseResult.CommandResult.Command.Name is "init" or "help")
    return await parseResult.InvokeAsync();

if (!Directory.Exists("Exercises")) {
    Console.WriteLine("""
           Welcome to...                             
      __ _                      _ _                  
     / _\ |__   __ _ _ __ _ __ | (_)_ __   __ _ ___  
     \ \| '_ \ / _` | '__| '_ \| | | '_ \ / _` / __| 
     _\ \ | | | (_| | |  | |_) | | | | | | (_| \__ \ 
     \__/_| |_|\__,_|_|  | .__/|_|_|_| |_|\__, |___/ 
                         |_|              |___/      
    
    The `Exercises/` directory couldn't be found in the current directory.
    If you are just starting with Sharplings, run the command `Sharplings init` to initialize it.
    """);
    return 1;
}

InfoFile infoFile = await InfoFile.ParseAsync();

if (infoFile.FormatVersion > currentFormatVersion) {
    throw new InvalidOperationException("""
    The format version specified in the `info.toml` file is higher than the last one supported.
    It is possible that you have an outdated version of Sharplings.
    Try to install the latest Sharplings version first.                                    
    """);
}

(AppState appState, StateFileStatus stateFileStatus) = await AppState.New(infoFile.Exercises, infoFile.FinalMessage);

if (!string.IsNullOrWhiteSpace(infoFile.WelcomeMessage) && stateFileStatus == StateFileStatus.NotRead) {
    Console.Clear();
    Console.WriteLine(infoFile.WelcomeMessage);
    Console.WriteLine("Press ENTER to continue ");
    Console.ReadLine();
    Console.Clear();
}

if (!AnsiConsole.Profile.Out.IsTerminal) {
    throw new InvalidOperationException("Unsupported or missing terminal/TTY");
}

if (args.Length != 0) {
    int exitCode = await parseResult.InvokeAsync();
    if (exitCode != 0) return exitCode;
}

Terminal terminal = new(new TerminalOutputData {
    CompletedExercisesCount = Random.Shared.Next(0, 95),
    AllExercisesCount = 94
});

await Task.Delay(-1);
return 0;

static RootCommand GenerateCommands() {
    RootCommand rootCommand = new("Sharplings is a collection of small exercises to get you used to reading and writing C# code. Inspired by Rustlings") {
        Subcommands = {
            new Command("init", "Initialize the official Sharplings exercises") {
                Action = new InitCommand()
            }
        }
    };

    return rootCommand;
}
