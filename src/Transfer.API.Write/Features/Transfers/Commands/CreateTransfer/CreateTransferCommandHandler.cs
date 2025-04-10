using MediatR;
using Transfer.API.Write.Infrastructure;
using Transfer.Shared.Models;

namespace Transfer.API.Write.Features.Transfers.Commands.CreateTransfer;

public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, int>
{
    private readonly TransferDbContext _context;

    public CreateTransferCommandHandler(TransferDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = new TransferEntity
        {
            FromAccount = request.FromAccount,
            ToAccount = request.ToAccount,
            Amount = request.Amount,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transfers.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }
} 