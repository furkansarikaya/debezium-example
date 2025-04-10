using MediatR;

namespace Transfer.API.Read.Features.Transfers.Queries.GetTransfer;

public class GetTransferQuery : IRequest<Models.Transfer?>
{
    public int Id { get; set; }
} 