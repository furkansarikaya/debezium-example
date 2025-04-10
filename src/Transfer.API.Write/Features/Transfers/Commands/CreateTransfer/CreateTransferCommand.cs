using MediatR;

namespace Transfer.API.Write.Features.Transfers.Commands.CreateTransfer;

public class CreateTransferCommand : IRequest<int>
{
    public int FromAccount { get; set; }
    public int ToAccount { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
} 