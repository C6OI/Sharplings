using JetBrains.Annotations;

namespace Exercises.Internal;

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithInheritors)]
public interface IExercise {
    [UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithInheritors)]
    void Run();
}
