using System.CommandLine;
using Sharplings.Commands;
using Spectre.Console;

ParseResult parseResult = GenerateCommands().Parse(args);

try {
    return await parseResult.InvokeAsync(new InvocationConfiguration {
        EnableDefaultExceptionHandler = false
    });
} catch (Exception e) {
    AnsiConsole.WriteException(e, new ExceptionSettings {
#if DEBUG
        Format = ExceptionFormats.Default,
#else
        Format = ExceptionFormats.NoStackTrace,
#endif
        Style = new ExceptionStyle {
            Message = Style.Parse("red")
        }
    });

    return e.HResult;
}

static RootCommand GenerateCommands() {
    RootCommand rootCommand = new("Sharplings is a collection of small exercises to get you used to reading and writing C# code. Inspired by Rustlings") {
        Subcommands = {
            new Command("init", "Initialize the official Sharplings exercises") {
                Action = new InitAction(),
                Options = {
                    new Option<DirectoryInfo>("--output", "-o") {
                        Description = "The name of the directory where the exercises will be initialized. Default is 'Sharplings'",
                        Required = false,
                        Arity = ArgumentArity.ExactlyOne
                    }
                }
            },
            new Command("run", "Run a single exercise. Runs the next pending exercise if the exercise name is not specified") {
                Action = new RootAction(Subcommand.Run),
                Arguments = {
                    new Argument<string>("name") {
                        Description = "The name of the exercise",
                        Arity = ArgumentArity.ZeroOrOne
                    }
                }
            },
            new Command("check-all", "Check all the exercises, marking them as done or pending accordingly") {
                Action = new RootAction(Subcommand.CheckAll)
            },
            new Command("reset", "Reset a single exercise") {
                Action = new RootAction(Subcommand.Reset),
                Arguments = {
                    new Argument<string>("name") {
                        Description = "The name of the exercise",
                        Arity = ArgumentArity.ExactlyOne
                    }
                }
            },
            new Command("hint", "Show a hint. Shows the hint of the next pending exercise if the exercise name is not specified") {
                Action = new RootAction(Subcommand.Hint),
                Arguments = {
                    new Argument<string>("name") {
                        Description = "The name of the exercise",
                        Arity = ArgumentArity.ZeroOrOne
                    }
                }
            }
        },
        Options = {
            new Option<bool>("--manual-run") {
                Description = "Manually run the current exercise using `r` in the watch mode. Only use this if Sharplings fails to detect exercise file changes",
                Arity = ArgumentArity.Zero
            }
        },
        Action = new RootAction(Subcommand.None)
    };

    return rootCommand;
}
