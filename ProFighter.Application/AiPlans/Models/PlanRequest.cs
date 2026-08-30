using System.ComponentModel.DataAnnotations;

namespace ProFighter.Application.AiPlans.Models;

public record PlanRequest(
    PlanType Type,
    ExerciseInput? Exercise,   // required when Type == Exercise
    FoodInput? Food);          // required when Type == Food

public record ExerciseInput(
    [Required, MaxLength(100)] string TargetArea);  // e.g. "Chest", "Full Body", "Legs"

public record FoodInput(
    [Range(20, 300)] decimal Weight,       // kg
    [Range(100, 250)] decimal Height,      // cm
    [Range(10, 100)] int Age,
    Gender Gender,
    FitnessGoal Goal,
    [Range(1000, 5000)] int? TargetKcal = null,    // user-entered, if null the AI calculates it
    [Range(1, 70)] decimal? BodyFatPercentage = null,
    [Range(1, 150)] decimal? MuscleMass = null);
