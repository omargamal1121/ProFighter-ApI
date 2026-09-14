using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Sync.Jobs;

namespace ProFighter.API.Controllers;

// ── Admin Endpoints (Commented out — user-facing endpoints only active) ──
/*
[Route("api/admin/progym/sync")]
public class AdminProGymSyncController : BaseController
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public AdminProGymSyncController(IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs;
    }

    /// <summary>
    /// Enqueues a Hangfire background job to sync Pro Gym customers from Rekaz.
    /// </summary>
    [HttpPost("customers")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status202Accepted)]
    public ActionResult<ApiResponse<string>> SyncProGymCustomers()
    {
        var jobId = _backgroundJobs.Enqueue<GymDataSyncJob>(job => job.SyncProGymCustomersAsync(CancellationToken.None));
        return HandleResult(Result<string>.Success(
            jobId, 
            "Pro Gym customer sync job has been enqueued successfully. Check Hangfire dashboard for status.", 
            StatusCodes.Status202Accepted));
    }

    /// <summary>
    /// Enqueues a Hangfire background job to sync Pro Gym subscriptions from Rekaz.
    /// </summary>
    [HttpPost("subscriptions")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status202Accepted)]
    public ActionResult<ApiResponse<string>> SyncProGymSubscriptions()
    {
        var jobId = _backgroundJobs.Enqueue<GymDataSyncJob>(job => job.SyncProGymSubscriptionsAsync(CancellationToken.None));
        return HandleResult(Result<string>.Success(
            jobId, 
            "Pro Gym subscription sync job has been enqueued successfully. Check Hangfire dashboard for status.", 
            StatusCodes.Status202Accepted));
    }
}
*/
