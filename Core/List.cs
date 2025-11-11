using System.Text;
using System.Threading.Channels;
using Sharplings.Terminal;
using Sharplings.Utils;
using Spectre.Console;
using Progress = Sharplings.Terminal.Progress;

namespace Sharplings;

static class List {
    public static async Task ShowList(AppState appState, CancellationToken cancellationToken) {
        await HandleList(appState, cancellationToken);
        AnsiConsole.Cursor.Show();
    }

    static async Task HandleList(AppState appState, CancellationToken cancellationToken) { // no scrolling handle :(
        State state = new(appState);
        bool isSearching = false;

        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try {
            await foreach (ITerminalEvent terminalEvent in TerminalHandler.EventReader.ReadAllAsync(cts.Token)) {
                if (cts.IsCancellationRequested) break;

                switch (terminalEvent) {
                    case Key(var info): {
                        state.Message = "";

                        if (isSearching) {
                            switch (info.Key) {
                                case ConsoleKey.Escape or ConsoleKey.Enter: {
                                    isSearching = false;
                                    state.SearchQuery = "";
                                    break;
                                }

                                case ConsoleKey.Backspace: {
                                    if (string.IsNullOrEmpty(state.SearchQuery)) break;

                                    state.SearchQuery = state.SearchQuery[..^1];
                                    state.ApplySearchQuery();
                                    break;
                                }

                                default: {
                                    state.SearchQuery += info.KeyChar;
                                    state.ApplySearchQuery();
                                    break;
                                }
                            }

                            state.Draw();
                            continue;
                        }

                        switch (info.Key) {
                            case ConsoleKey.Q: {
                                await cts.CancelAsync();
                                continue;
                            }

                            case ConsoleKey.DownArrow or ConsoleKey.J: {
                                state.SelectNext();
                                break;
                            }

                            case ConsoleKey.UpArrow or ConsoleKey.K: {
                                state.SelectPrevious();
                                break;
                            }

                            case ConsoleKey.G when info.KeyChar == 'g':
                            case ConsoleKey.Home: {
                                state.SelectFirst();
                                break;
                            }

                            case ConsoleKey.G when info.KeyChar == 'G':
                            case ConsoleKey.End: {
                                state.SelectLast();
                                break;
                            }

                            case ConsoleKey.D: {
                                if (state.Filter == Filter.Done) {
                                    state.Filter = Filter.None;
                                    state.Message = "Disabled filter DONE";
                                } else {
                                    state.Filter = Filter.Done;
                                    state.Message = "Enabled filter DONE │ Press d again to disable the filter";
                                }
                                break;
                            }

                            case ConsoleKey.P: {
                                if (state.Filter == Filter.Pending) {
                                    state.Filter = Filter.None;
                                    state.Message = "Disabled filter PENDING";
                                } else {
                                    state.Filter = Filter.Pending;
                                    state.Message = "Enabled filter PENDING │ Press p again to disable the filter";
                                }
                                break;
                            }

                            case ConsoleKey.R: {
                                await state.ResetSelected();
                                break;
                            }

                            case ConsoleKey.C: {
                                bool success = await state.SelectedToCurrentExercise();

                                if (success) {
                                    await cts.CancelAsync();
                                    continue;
                                }

                                break;
                            }

                            case ConsoleKey.S or ConsoleKey.Divide: {
                                isSearching = true;
                                state.ApplySearchQuery();
                                break;
                            }

                            // Redraw to remove the message.
                            case ConsoleKey.Escape: break;

                            default: continue;
                        }

                        break;
                    }

                    case Resize(var width, var height):
                        state.SetTerminalSize(width, height);
                        break;

                    default: throw new ArgumentOutOfRangeException(nameof(terminalEvent));
                }

                state.Draw();
            }
        } catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        catch (ChannelClosedException) { }
    }

    class State {
        const int ColumnSpacing = 2;

        public State(AppState appState) {
            AnsiConsole.Clear();
            AnsiConsole.Cursor.Hide();

            (int nameColumnWidth, int pathColumnWidth) = appState.Exercises.Aggregate((4, 4),
                (tuple, exercise) => (tuple.Item1.Max(exercise.Name.Length), tuple.Item2.Max(exercise.Path.Length)));

            NameColumnPadding = new string(' ', nameColumnWidth + ColumnSpacing);
            PathColumnPadding = new string(' ', pathColumnWidth);

            int rowsWithFilterCount = appState.Exercises.Count;
            int selectedIndex = appState.CurrentExerciseIndex;

            (int width, int height) = (AnsiConsole.Profile.Width, AnsiConsole.Profile.Height);
            ScrollState = new ScrollState(rowsWithFilterCount, selectedIndex, 5);

            AppState = appState;

            SetTerminalSize(width, height);
            Draw();
        }

        public Filter Filter {
            get;
            set {
                field = value;
                UpdateRows();
            }
        }

        public string Message { get; set; } = "";
        public string SearchQuery { get; set; } = "";
        AppState AppState { get; }
        ScrollState ScrollState { get; }
        StringBuilder DrawBuffer { get; } = new();
        string NameColumnPadding { get; }
        string PathColumnPadding { get; }
        int Width { get; set; }
        int Height { get; set; }
        bool ShowFooter { get; set; } = true;

        public void SelectNext() => ScrollState.SelectNext();
        public void SelectPrevious() => ScrollState.SelectPrevious();
        public void SelectFirst() => ScrollState.SelectFirst();
        public void SelectLast() => ScrollState.SelectLast();

        /// Return `true` if there was something to select.
        public async Task<bool> SelectedToCurrentExercise() {
            if (ScrollState.SelectedIndex == -1) {
                Message = "Nothing selected to continue at!";
                return false;
            }

            int exerciseIndex = SelectedToExerciseIndex(ScrollState.SelectedIndex);
            await AppState.SetCurrentExerciseIndex(exerciseIndex);
            return true;
        }

        public async Task ResetSelected() {
            if (ScrollState.SelectedIndex == -1) {
                Message = "Nothing selected to reset!";
                return;
            }

            int exerciseIndex = SelectedToExerciseIndex(ScrollState.SelectedIndex);
            string exerciseName = await AppState.ResetExerciseByIndex(exerciseIndex);

            UpdateRows();
            Message = $"The exercise `{exerciseName}` has been reset";
        }

        public void ApplySearchQuery() {
            Message = $"search: {SearchQuery}|";

            if (string.IsNullOrWhiteSpace(SearchQuery))
                return;

            Func<Exercise, bool> predicate = exercise => exercise.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);

            int index = Filter switch {
                Filter.None => AppState.Exercises.FindIndex(predicate),
                Filter.Pending => AppState.Exercises.Where(exercise => !exercise.Done).FindIndex(predicate),
                Filter.Done => AppState.Exercises.Where(exercise => exercise.Done).FindIndex(predicate),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (index != -1) {
                ScrollState.SelectedIndex = index;
            } else {
                Message += " (not found)";
            }
        }

        void UpdateRows() => ScrollState.RowsCount = Filter switch {
            Filter.None => AppState.Exercises.Count,
            Filter.Pending => AppState.Exercises.Count(exercise => !exercise.Done),
            Filter.Done => AppState.Exercises.Count(exercise => exercise.Done),
            _ => throw new ArgumentOutOfRangeException()
        };

        public void SetTerminalSize(int width, int height) {
            Width = width;
            Height = height;
            DrawBuffer.EnsureCapacity(width * height);

            if (height == 0) return;

            const int headerHeight = 1;
            // 1 progress bar, 2 footer message lines.
            const int footerHeight = 3;
            ShowFooter = height > headerHeight + footerHeight;

            ScrollState.MaxRowsCountToDisplay = height.SaturatingSub(headerHeight + ShowFooter.Into<int>() * footerHeight);
        }

        public void Draw() {
            if (Height == 0) return;

            DrawBuffer.Clear();

            MaxLengthWriter writer = new(Width);
            writer.Write("  Current  State    Name");
            writer.Write(NameColumnPadding[4..]);
            writer.Write("Path");
            BufferAppendClearLine(writer);

            int displayedRows = Filter switch {
                Filter.None => DrawRows(AppState.Exercises),
                Filter.Pending => DrawRows(AppState.Exercises.Where(exercise => !exercise.Done)),
                Filter.Done => DrawRows(AppState.Exercises.Where(exercise => exercise.Done)),
                _ => throw new ArgumentOutOfRangeException()
            };

            for (int i = 0; i < ScrollState.MaxRowsCountToDisplay - displayedRows; i++)
                BufferAppendClearLine();

            if (ShowFooter) {
                string progressBar = Progress.BuildProgressBar(AppState.ExercisesDone, AppState.Exercises.Count, Width);
                BufferAppendClearLine(progressBar);

                writer.Reset();

                if (string.IsNullOrWhiteSpace(Message)) {
                    if (ScrollState.SelectedIndex != -1) {
                        writer.Write("↓/j ↑/k home/g end/G | <c>ontinue at | <r>eset exercise");
                        BufferAppendClearLine(writer);

                        writer.Reset();
                        writer.Write("<s>earch | filter ");
                    } else {
                        // Nothing selected (and nothing shown), so only display filter and quit.
                        writer.Write("filter ");
                    }

                    switch (Filter) {
                        case Filter.None:
                            writer.Write("<d>one/<p>ending");
                            break;

                        case Filter.Pending: {
                            writer.Write("[fuchsia underline]", true);
                            writer.Write("<d>one");
                            writer.Write("[/]", true);
                            writer.Write("/<p>ending");
                            break;
                        }

                        case Filter.Done:
                            writer.Write("<d>one/");
                            writer.Write("[fuchsia underline]", true);
                            writer.Write("<d>one");
                            writer.Write("[/]", true);
                            break;

                        default: throw new ArgumentOutOfRangeException();
                    }

                    writer.Write(" | <q>uit list");

                    BufferAppendClear(writer);
                } else {
                    writer.Write("[fuchsia]", true);
                    writer.Write(Message);
                    writer.Write("[/]", true);

                    BufferAppendClearLine(writer);
                    BufferAppendClear();
                }
            }

            AnsiConsole.Cursor.SetPosition(0, 0);
            AnsiConsole.Markup(DrawBuffer.ToString());
        }

        int SelectedToExerciseIndex(int selected) => Filter switch {
            Filter.None => selected,
            Filter.Pending => AppState.Exercises.Index().Where(tuple => !tuple.Item.Done).ElementAt(selected).Index,
            Filter.Done => AppState.Exercises.Index().Where(tuple => tuple.Item.Done).ElementAt(selected).Index,
            _ => throw new ArgumentOutOfRangeException()
        };

        int DrawRows(IEnumerable<Exercise> exercises) {
            int currentExerciseIndex = AppState.CurrentExerciseIndex;
            int rowOffset = ScrollState.Offset;

            int displayedRows = 0;

            // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
            foreach ((int index, Exercise exercise) in exercises
                         .Skip(rowOffset)
                         .Take(ScrollState.MaxRowsCountToDisplay)
                         .Index()) {
                MaxLengthWriter writer = new(Width);

                if (ScrollState.SelectedIndex == rowOffset + displayedRows) {
                    writer.Write("* ");
                    writer.Write("[invert bold]", true);
                } else {
                    writer.Write("  ");
                    writer.Write("[]", true);
                }

                if (index != currentExerciseIndex) writer.Write("         ");
                else {
                    writer.Write("[red]", true);
                    writer.Write(">>>>>>>  ");
                    writer.Write("[/]", true);
                }

                if (exercise.Done) {
                    writer.Write("[lime]", true);
                    writer.Write("DONE   ");
                } else {
                    writer.Write("[yellow]", true);
                    writer.Write("PENDING");
                }
                writer.Write("[/]", true);
                writer.Write("  ");

                WriteExerciseName(writer, exercise);
                writer.Write(NameColumnPadding[exercise.Name.Length..]);

                writer.Write("[blue]", true);
                writer.Write(exercise.Path);
                writer.Write("[/]", true);

                writer.Write(PathColumnPadding[exercise.Path.Length..]);

                writer.FillRemainingLength(' ');
                writer.Write("[/]", true);

                BufferAppendClearLine(writer);
                displayedRows++;
            }
            // ReSharper restore ConvertIfStatementToConditionalTernaryExpression

            return displayedRows;
        }

        void WriteExerciseName(MaxLengthWriter writer, Exercise exercise) {
            if (!string.IsNullOrWhiteSpace(SearchQuery)) {
                int index = exercise.Name.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase);

                if (index != -1) {
                    // ReSharper disable ReplaceSubstringWithRangeIndexer
                    string preHighlight = exercise.Name.Substring(0, index);
                    string highlight = exercise.Name.Substring(index, SearchQuery.Length);
                    string postHighlight = exercise.Name.Substring(index + SearchQuery.Length);
                    // ReSharper restore ReplaceSubstringWithRangeIndexer

                    writer.Write(preHighlight);
                    writer.Write("[fuchsia]", true);
                    writer.Write(highlight);
                    writer.Write("[/]", true);
                    writer.Write(postHighlight);
                    return;
                }
            }

            writer.Write(exercise.Name);
        }

        StringBuilder BufferAppendClear() =>
            DrawBuffer.Append(new string(' ', Width));

        StringBuilder BufferAppendClear(string value) =>
            DrawBuffer.Append(value).Append(new string(' ', Width.SaturatingSub(value.Length)));

        StringBuilder BufferAppendClearLine() =>
            DrawBuffer.AppendLine(new string(' ', Width));

        StringBuilder BufferAppendClearLine(string value) =>
            DrawBuffer.Append(value).AppendLine(new string(' ', Width.SaturatingSub(value.Length)));
    }

    class ScrollState(int rowsCount, int selectedIndex, int maxScrollPadding) {
        public int MaxRowsCountToDisplay {
            get;
            set {
                field = value;
                UpdateScrollPadding();
                UpdateOffset();
            }
        } = 0;

        public int SelectedIndex {
            get;
            set {
                field = value;
                UpdateOffset();
            }
        } = selectedIndex;

        public int RowsCount {
            get;
            set {
                field = value;

                if (field == 0) {
                    SelectedIndex = -1;
                } else if (SelectedIndex != -1) {
                    SelectedIndex = SelectedIndex.Min(RowsCount - 1);
                } else {
                    SelectedIndex = 0;
                }
            }
        } = rowsCount;

        int MaxScrollPadding { get; } = maxScrollPadding;
        public int Offset { get; private set; } = selectedIndex == -1 ? 0 : selectedIndex.SaturatingSub(maxScrollPadding);
        int ScrollPadding { get; set; }

        public void SelectNext() {
            if (SelectedIndex != -1)
                SelectedIndex = (SelectedIndex + 1).Min(RowsCount - 1);
        }

        public void SelectPrevious() {
            if (SelectedIndex != -1)
                SelectedIndex = SelectedIndex.SaturatingSub(1);
        }

        public void SelectFirst() {
            if (RowsCount > 0)
                SelectedIndex = 0;
        }

        public void SelectLast() {
            if (RowsCount > 0)
                SelectedIndex = RowsCount - 1;
        }

        void UpdateOffset() {
            if (SelectedIndex == -1) return;

            int minOffset = (SelectedIndex + ScrollPadding).SaturatingSub(MaxRowsCountToDisplay.SaturatingSub(1));
            int maxOffset = SelectedIndex.SaturatingSub(ScrollPadding);
            int globalMaxOffset = RowsCount.SaturatingSub(MaxRowsCountToDisplay);

            Offset = Offset
                .Max(minOffset)
                .Min(maxOffset)
                .Min(globalMaxOffset);
        }

        void UpdateScrollPadding() =>
            ScrollPadding = (MaxRowsCountToDisplay / 4).Min(MaxScrollPadding);
    }

    enum Filter {
        None,
        Pending,
        Done
    }
}
