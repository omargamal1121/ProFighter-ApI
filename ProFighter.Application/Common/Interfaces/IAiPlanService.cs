using ProFighter.Application.AiPlans.Models;

namespace ProFighter.Application.Common.Interfaces;

public interface IAiPlanService
{
    Task<ExercisePlanResponse> GenerateExercisePlanAsync(ExerciseInput input, CancellationToken ct);
    Task<FoodPlanResponse> GenerateFoodPlanAsync(FoodInput input, CancellationToken ct);
}
