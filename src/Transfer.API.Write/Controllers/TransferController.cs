using MediatR;
using Microsoft.AspNetCore.Mvc;
using Transfer.API.Write.Features.Transfers.Commands.CreateTransfer;

namespace Transfer.API.Write.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransferController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreateTransfer(CreateTransferCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
} 