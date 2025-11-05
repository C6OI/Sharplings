using Spectre.Console;

namespace Sharplings.Terminal;

class CheckProgressVisualiser {
    const string CheckingColor = "blue";
    const string DoneColor = "lime";
    const string PendingColor = "red";

    public CheckProgressVisualiser(int width) {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine("Checking all exercises...");

        AnsiConsole.Write("Color of exercise number: ");
        AnsiConsole.Markup($"[{CheckingColor}]Checking[/] - ");
        AnsiConsole.Markup($"[{DoneColor}]Done[/] - ");
        AnsiConsole.Markup($"[{PendingColor}]Pending[/]");
        AnsiConsole.WriteLine();

        // Exercise numbers with up to 3 digits.
        // +1 because the last column doesn't end with a whitespace.
        ColumnsCount = (width + 1) / 4;
    }

    int ColumnsCount { get; }

    public void Update(IList<CheckProgress> progresses) {
        AnsiConsole.Cursor.SetPosition(0, 3);

        int exerciseNum = 1;

        foreach (CheckProgress exerciseProgress in progresses) {
            switch (exerciseProgress) {
                case CheckProgress.None: break;

                case CheckProgress.Checking:
                    AnsiConsole.Markup($"[{CheckingColor}]{exerciseNum,-3}[/]");
                    break;

                case CheckProgress.Done:
                    AnsiConsole.Markup($"[{DoneColor}]{exerciseNum,-3}[/]");
                    break;

                case CheckProgress.Pending:
                    AnsiConsole.Markup($"[{PendingColor}]{exerciseNum,-3}[/]");
                    break;

                default: throw new ArgumentOutOfRangeException();
            }

            if (exerciseNum != progresses.Count) {
                if (exerciseNum % ColumnsCount == 0) {
                    AnsiConsole.WriteLine();
                } else {
                    AnsiConsole.Write(' ');
                }

                exerciseNum++;
            }
        }
    }
}

enum CheckProgress {
    None,
    Checking,
    Done,
    Pending
}
