using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Auth.Commands.CompleteAccount;
using ProFighter.Application.Auth.Commands.CompleteFirstLogin;
using ProFighter.Application.Auth.Commands.ConfirmEmail;
using ProFighter.Application.Auth.Commands.ForgotPassword;
using ProFighter.Application.Auth.Commands.Login;
using ProFighter.Application.Auth.Commands.RequestEmailConfirmation;
using ProFighter.Application.Auth.Commands.ResetPassword;
using ProFighter.Application.Common;
using ProFighter.Application.Customers.Commands.RegisterCustomer;
using System.Security.Claims;

namespace ProFighter.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : BaseController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Login with mobile number and password.
    /// Returns a first-login setup token when IsFirstLogin is true,
    /// or normal JWT token otherwise.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResult>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<LoginResult>.CreateSuccessResponse("Login successful.", result, 200));
    }

    /// <summary>
    /// Register a new customer account.
    /// Creates customer in Rekaz first, then locally with the provided credentials.
    /// Returns customer ID and email confirmation status.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegisterCustomerResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RegisterCustomerResult>>> Register(
        [FromBody] RegisterCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<RegisterCustomerResult>.CreateSuccessResponse("Registration successful.", result, 200));
    }

    /// <summary>
    /// Complete account setup (new password + email).
    /// Requires a valid account-completion JWT (token_type = account_completion).
    /// After success, returns a normal access + refresh token pair.
    /// </summary>
    [HttpPost("complete-account")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CompleteAccountResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CompleteAccountResult>>> CompleteAccount(
        [FromBody] CompleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        // Guard: only account-completion tokens may reach this endpoint
        var tokenType = User.FindFirstValue("token_type");
        if (tokenType != "account_completion")
            return StatusCode(403, ApiResponse<object>.CreateErrorResponse(
                "Forbidden",
                new ErrorResponse("Forbidden", "This endpoint requires an account-completion token."),
                403));

        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized(ApiResponse<object>.CreateErrorResponse(
                "Unauthorized",
                new ErrorResponse("Unauthorized", "User identity could not be determined."),
                401));

        var command = new CompleteAccountCommand(userId.Value, request.NewPassword, request.Email);
        var result  = await _mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<CompleteAccountResult>.CreateSuccessResponse("Account completed successfully.", result, 200));
    }

    /// <summary>
    /// Complete first login setup (new password + email).
    /// Requires a valid first-login setup token.
    /// After success, returns a normal JWT token.
    /// </summary>
    [HttpPost("complete-first-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CompleteFirstLoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CompleteFirstLoginResult>>> CompleteFirstLogin(
        [FromBody] CompleteFirstLoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<CompleteFirstLoginResult>.CreateSuccessResponse("First login completed successfully.", result, 200));
    }

    /// <summary>
    /// Forgot password - initiates password reset flow.
    /// Returns different outcomes based on email confirmation status.
    /// No [Authorize] - uses mobile number as identifier.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResult>>> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse<ForgotPasswordResult>.CreateSuccessResponse("Password reset request processed.", result, 200));
    }

    /// <summary>
    /// Reset password using OTP - completes password reset flow.
    /// Requires mobile number, OTP, and new password.
    /// No [Authorize] - OTP serves as authentication.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordWithOtpResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ResetPasswordWithOtpResult>>> ResetPassword(
        [FromBody] ResetPasswordWithOtpCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.Success)
        {
            return Ok(ApiResponse<ResetPasswordWithOtpResult>.CreateSuccessResponse(result.Message, result, 200));
        }
        else
        {
            return BadRequest(ApiResponse<ResetPasswordWithOtpResult>.CreateErrorResponse(
                result.Message,
                new ErrorResponse("Password Reset Failed", result.Message),
                400));
        }
    }

    /// <summary>
    /// Confirm email using OTP - completes email confirmation flow.
    /// Requires mobile number and OTP.
    /// No [Authorize] - OTP serves as authentication.
    /// </summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ConfirmEmailWithOtpResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ConfirmEmailWithOtpResult>>> ConfirmEmail(
        [FromBody] ConfirmEmailWithOtpCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.Success)
        {
            return Ok(ApiResponse<ConfirmEmailWithOtpResult>.CreateSuccessResponse(result.Message, result, 200));
        }
        else
        {
            return BadRequest(ApiResponse<ConfirmEmailWithOtpResult>.CreateErrorResponse(
                result.Message,
                new ErrorResponse("Email Confirmation Failed", result.Message),
                400));
        }
    }

    /// <summary>
    /// Request email confirmation OTP - initiates email confirmation flow.
    /// Requires mobile number.
    /// No [Authorize] - uses mobile number as identifier.
    /// </summary>
    [HttpPost("request-email-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RequestEmailConfirmationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RequestEmailConfirmationResult>>> RequestEmailConfirmation(
        [FromBody] RequestEmailConfirmationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.Success)
        {
            return Ok(ApiResponse<RequestEmailConfirmationResult>.CreateSuccessResponse(result.Message, result, 200));
        }
        else
        {
            return BadRequest(ApiResponse<RequestEmailConfirmationResult>.CreateErrorResponse(
                result.Message,
                new ErrorResponse("Email Confirmation Request Failed", new List<string> { result.Message }),
                400));
        }
    }
}

/// <summary>Request body for complete-account endpoint. UserId is sourced from the JWT claim, not the body.</summary>
public record CompleteAccountRequest(string NewPassword, string Email);
