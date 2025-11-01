namespace Sharplings.Utils;

public static class EmbeddedFilesUtils {
    public static async Task InitExercisesDirAsync(this EmbeddedFiles embeddedFiles, IList<ExerciseInfo> exerciseInfos, CancellationToken cancellationToken = default) {
        Directory.CreateDirectory("Exercises");
        await File.WriteAllBytesAsync(Path.Combine("Exercises", "README.md"), embeddedFiles.Files["ExercisesReadme"], cancellationToken);

        foreach (ExerciseDir exerciseDir in embeddedFiles.ExerciseDirs)
            await exerciseDir.InitOnDiskAsync(cancellationToken);

        foreach ((ExerciseInfo exerciseInfo, ExerciseFiles exerciseFiles) in exerciseInfos.Zip(embeddedFiles.ExerciseFiles)) {
            ExerciseDir dir = embeddedFiles.ExerciseDirs[exerciseFiles.DirInd];
            string exercisePath = Path.Combine("Exercises", dir.Name, $"{exerciseInfo.Name}.cs");

            await File.WriteAllBytesAsync(exercisePath, exerciseFiles.Exercise, cancellationToken);
        }
    }

    public static async Task InitOnDiskAsync(this ExerciseDir exerciseDir, CancellationToken cancellationToken = default) {
        string dirPath = Path.Combine("Exercises", exerciseDir.Name);
        Directory.CreateDirectory(dirPath);

        string readmePath = Path.Combine(dirPath, "README.md");
        await File.WriteAllBytesAsync(readmePath, exerciseDir.Readme, cancellationToken);
    }
}
