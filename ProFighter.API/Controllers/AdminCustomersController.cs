using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProFighter.Application.Common;
using ProFighter.Application.Customers.Commands.CreateCustomerByAdmin;
using ProFighter.Application.Customers.Commands.ImportCustomersFromRekaz;

namespace ProFighter.API.Controllers;

[Route("api/admin/customers")]
public class AdminCustomersController : BaseController
{
    private readonly ISender _mediator;

    public AdminCustomersController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Commands a bulk import of all customers from Rekaz to local database.
    /// </summary>
    [HttpPost("import-from-rekaz")]
    [ProducesResponseType(typeof(ApiResponse<ImportCustomersResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ImportCustomersResult>>> ImportFromRekaz(CancellationToken ct)
    {
        var result = await _mediator.Send(new ImportCustomersFromRekazCommand(), ct);
        return HandleResult(Result<ImportCustomersResult>.Success(result, "Bulk import completed."));
    }

    /// <summary>
    /// Creates a customer first on Rekaz, and then attempts local database sync.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateCustomerByAdminResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateCustomerByAdminResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CreateCustomerByAdminResult>>> CreateCustomer(
        [FromBody] CreateCustomerByAdminCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);

        if (result.LocalSyncSucceeded)
        {
            return HandleResult(Result<CreateCustomerByAdminResult>.Success(
                result, 
                "Customer created and synchronized successfully.", 
                StatusCodes.Status201Created));
        }

        // Rekaz succeeded but local failed: return 200 with warnings
        return HandleResult(Result<CreateCustomerByAdminResult>.Success(
            result, 
            "Customer created on Rekaz but local synchronization failed. A background retry has been scheduled.", 
            StatusCodes.Status200OK));
    }
}
