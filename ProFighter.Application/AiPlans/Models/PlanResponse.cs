namespace ProFighter.Application.AiPlans.Models;

public record ExercisePlanResponse(
    string PlanTitle,
    string Notes,
    List<ExerciseItem> Exercises);

public record ExerciseItem(string Name, int Sets, string Reps, int RestSeconds);

public record FoodPlanResponse(
    string PlanTitle,
    int DailyCalorieTarget,
    MacroSplit MacroSplit,
    List<MealItem> SampleMeals,
    string Notes);

public record MacroSplit(int ProteinPercent, int CarbsPercent, int FatPercent);
public record MealItem(string Meal, string Example, int ApproxKcal);
