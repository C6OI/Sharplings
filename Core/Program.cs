using System.CommandLine;
using Sharplings.Commands;
using Sharplings.Terminal;

if (args.Length != 0) {
    ParseResult parseResult = GenerateCommands().Parse(args);
    int exitCode = await parseResult.InvokeAsync();

    if (exitCode != 0) return exitCode;
    if (parseResult.Action != null) return 0;
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
