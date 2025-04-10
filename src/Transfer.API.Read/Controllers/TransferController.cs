using MediatR;
using Microsoft.AspNetCore.Mvc;
using Transfer.API.Read.Features.Transfers.Queries.GetTransfer;

namespace Transfer.API.Read.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransferController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Features.Transfers.Models.Transfer?>> GetTransfer(int id)
    {
        var query = new GetTransferQuery { Id = id };
        var result = await _mediator.Send(query);
        return result != null ? Ok(result) : NotFound();
    }
} 