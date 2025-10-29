using System.CommandLine;
using Sharplings.Terminal;

ParseResult parseResult = GenerateCommands().Parse(args);
int exitCode = await parseResult.InvokeAsync();

if (exitCode != 0)
    return exitCode;

if (parseResult.Action != null)
    return 0;

Terminal terminal = new(new TerminalOutputData {
    CompletedExercisesCount = Random.Shared.Next(0, 95),
    AllExercisesCount = 94
});

while (true) {
    terminal.OutputData = new TerminalOutputData {
        CompletedExercisesCount = Random.Shared.Next(0, 95),
        AllExercisesCount = 94,
        ExerciseDone = true,
        ModuleName = "05_test",
        ScriptFile = "HelloWorld.cs"
    };

    await Task.Delay(Random.Shared.Next(1000, 2000));
}

await Task.Delay(-1);
return 0;

static RootCommand GenerateCommands() {
    RootCommand rootCommand =
        new("Sharplings is a collection of small exercises to get you used to writing and reading C# code. Inspired by Rustlings");

    return rootCommand;
}
