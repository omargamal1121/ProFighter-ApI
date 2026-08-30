using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using ProFighter.Application.AiPlans.Models;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Infrastructure.Services;

public class AiPlanService : IAiPlanService
{
    private readonly ChatClient _chatClient;

    public AiPlanService(IConfiguration configuration)
    {
        // NOTE: This endpoint is currently pointed at an OpenAI-compatible free-tier provider for testing.
        // The JSON-mode/structured-output parameter support should be verified against that specific provider's docs.
        // Some OpenAI-compatible providers support the full JSON schema enforcement OpenAI offers, others only 
        // support a looser "json_object" mode, which could affect how strictly the response matches the target shape.
        var baseUrl = configuration["AiProvider:BaseUrl"] ?? throw new ArgumentNullException("AiProvider:BaseUrl is missing");
        var apiKey = configuration["AiProvider:ApiKey"] ?? throw new ArgumentNullException("AiProvider:ApiKey is missing");
        var model = configuration["AiProvider:Model"] ?? throw new ArgumentNullException("AiProvider:Model is missing");

        var options = new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri(baseUrl)
        };

        var openAiClient = new OpenAI.OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), options);
        _chatClient = openAiClient.GetChatClient(model);
    }

    public async Task<ExercisePlanResponse> GenerateExercisePlanAsync(ExerciseInput input, CancellationToken ct)
    {
        var systemMessage = @"Generate a simple workout plan targeting: {targetArea}.

Provide 5-8 exercises suitable for a general gym-goer. Do not give medical advice. If the target area described is unclear or implies an injury/unsafe context, default to a general safe full-body plan and mention that in the notes field instead of refusing.
CRITICAL: ALL text values in the JSON output (including planTitle, notes, and the name of each exercise) MUST be written ONLY in Arabic. Do NOT include any English translations or text anywhere.";

        var userMessage = $"Target area: {input.TargetArea}";

        var schemaJson = @"{
          ""type"": ""object"",
          ""properties"": {
            ""planTitle"": { ""type"": ""string"" },
            ""notes"": { ""type"": ""string"" },
            ""exercises"": {
              ""type"": ""array"",
              ""items"": {
                ""type"": ""object"",
                ""properties"": {
                  ""name"": { ""type"": ""string"" },
                  ""sets"": { ""type"": ""integer"" },
                  ""reps"": { ""type"": ""string"" },
                  ""restSeconds"": { ""type"": ""integer"" }
                },
                ""required"": [""name"", ""sets"", ""reps"", ""restSeconds""],
                ""additionalProperties"": false
              }
            }
          },
          ""required"": [""planTitle"", ""notes"", ""exercises""],
          ""additionalProperties"": false
        }";

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "exercise_plan",
                jsonSchema: BinaryData.FromString(schemaJson),
                jsonSchemaIsStrict: true
            ),
            Temperature = 0.7f
        };

        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                new ChatMessage[]
                {
                    new SystemChatMessage(systemMessage),
                    new UserChatMessage(userMessage)
                },
                options,
                ct);

            var json = completion.Value.Content[0].Text;
            var response = JsonSerializer.Deserialize<ExercisePlanResponse>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (response == null)
            {
                throw new Exception("Deserialization returned null.");
            }

            return response;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to generate exercise plan from AI.", ex);
        }
    }

    public async Task<FoodPlanResponse> GenerateFoodPlanAsync(FoodInput input, CancellationToken ct)
    {
        var systemMessage = @"Generate a simple, general food/meal plan guideline based on user input.

The sampleMeals' approxKcal values should roughly sum to dailyCalorieTarget. This is general fitness guidance, not medical or clinical nutrition advice. Do not give exact gram-level meal plans, or supplement/medication dosing or names. Always include in 'notes' a recommendation to consult a nutritionist or doctor before starting, especially with any health condition. If the inputs (e.g. resulting BMI) suggest an extreme or medically concerning profile, still return conservative general guidance and strengthen the consult-a-professional note — do not refuse to generate a plan.
CRITICAL: ALL text values in the JSON output (including planTitle, meal examples, meal names, and notes) MUST be written ONLY in Arabic. Do NOT include any English translations or text anywhere.";

        var bodyFatStr = input.BodyFatPercentage.HasValue ? input.BodyFatPercentage.Value.ToString() : "not provided";
        var muscleMassStr = input.MuscleMass.HasValue ? input.MuscleMass.Value.ToString() : "not provided";
        var targetKcalStr = input.TargetKcal.HasValue 
            ? $"{input.TargetKcal.Value} kcal (provided by the user — build the plan around this exact target, do not recalculate or override it)" 
            : "not provided (please calculate an appropriate daily calorie target based on the user's profile and goal)";

        var userMessage = $@"Weight: {input.Weight} kg, Height: {input.Height} cm, Age: {input.Age}, Gender: {input.Gender}
Goal: {input.Goal}
Target daily calories: {targetKcalStr}
Body fat %: {bodyFatStr}
Muscle mass: {muscleMassStr}";

        var schemaJson = @"{
          ""type"": ""object"",
          ""properties"": {
            ""planTitle"": { ""type"": ""string"" },
            ""dailyCalorieTarget"": { ""type"": ""integer"" },
            ""macroSplit"": {
              ""type"": ""object"",
              ""properties"": {
                ""proteinPercent"": { ""type"": ""integer"" },
                ""carbsPercent"": { ""type"": ""integer"" },
                ""fatPercent"": { ""type"": ""integer"" }
              },
              ""required"": [""proteinPercent"", ""carbsPercent"", ""fatPercent""],
              ""additionalProperties"": false
            },
            ""sampleMeals"": {
              ""type"": ""array"",
              ""items"": {
                ""type"": ""object"",
                ""properties"": {
                  ""meal"": { ""type"": ""string"" },
                  ""example"": { ""type"": ""string"" },
                  ""approxKcal"": { ""type"": ""integer"" }
                },
                ""required"": [""meal"", ""example"", ""approxKcal""],
                ""additionalProperties"": false
              }
            },
            ""notes"": { ""type"": ""string"" }
          },
          ""required"": [""planTitle"", ""dailyCalorieTarget"", ""macroSplit"", ""sampleMeals"", ""notes""],
          ""additionalProperties"": false
        }";

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "food_plan",
                jsonSchema: BinaryData.FromString(schemaJson),
                jsonSchemaIsStrict: true
            ),
            Temperature = 0.7f
        };

        try
        {
            var completion = await _chatClient.CompleteChatAsync(
                new ChatMessage[]
                {
                    new SystemChatMessage(systemMessage),
                    new UserChatMessage(userMessage)
                },
                options,
                ct);

            var json = completion.Value.Content[0].Text;
            var response = JsonSerializer.Deserialize<FoodPlanResponse>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (response == null)
            {
                throw new Exception("Deserialization returned null.");
            }

            return response;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to generate food plan from AI.", ex);
        }
    }
}
