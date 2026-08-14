using MediatR;

namespace ProFighter.Application.Customers.Commands.ImportCustomersFromRekaz;

public record ImportCustomersFromRekazCommand : IRequest<ImportCustomersResult>;

public record ImportCustomersResult(
    int TotalFetched,
    int Imported,
    int Skipped,
    int Failed,
    List<string> Errors);
