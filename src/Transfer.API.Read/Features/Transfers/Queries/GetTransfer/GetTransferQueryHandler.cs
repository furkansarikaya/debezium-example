using MediatR;
using Transfer.API.Read.Infrastructure;

namespace Transfer.API.Read.Features.Transfers.Queries.GetTransfer;

public class GetTransferQueryHandler : IRequestHandler<GetTransferQuery, Models.Transfer?>
{
    private readonly RedisService _redisService;

    public GetTransferQueryHandler(RedisService redisService)
    {
        _redisService = redisService;
    }

    public async Task<Models.Transfer?> Handle(GetTransferQuery request, CancellationToken cancellationToken)
    {
        return await _redisService.GetTransferAsync(request.Id);
    }
} 