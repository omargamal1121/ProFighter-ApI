using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.AiPlans.Models;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.API.Controllers;

//[Authorize]
[ApiController]
[Route("api/ai")]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("AiPlanPolicy")]
public class AiPlanController : ControllerBase
{
    private readonly IAiPlanService _aiPlanService;

    public AiPlanController(IAiPlanService aiPlanService)
    {
        _aiPlanService = aiPlanService;
    }

    [HttpPost("plan")]
    public async Task<ActionResult<object>> GeneratePlan([FromBody] PlanRequest request, CancellationToken ct)
    {
        return request.Type switch
        {
            PlanType.Exercise when request.Exercise is not null
                => Ok(await _aiPlanService.GenerateExercisePlanAsync(request.Exercise, ct)),
            PlanType.Food when request.Food is not null
                => Ok(await _aiPlanService.GenerateFoodPlanAsync(request.Food, ct)),
            _ => BadRequest(new { Message = "Missing required input for the selected plan type." })
        };
    }

    [HttpGet("enums")]
    [AllowAnonymous] // Optionally allow without auth, or remove this if you want it protected
    public ActionResult GetEnums()
    {
        return Ok(new
        {
            PlanTypes = Enum.GetNames<PlanType>(),
            FitnessGoals = Enum.GetNames<FitnessGoal>(),
            Genders = Enum.GetNames<Gender>()
        });
    }
}
