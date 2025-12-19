using BCBGames.Application.Commands.Deposit;
using BCBGames.Application.Commands.Purchase;
using BCBGames.Application.Commands.Withdraw;
using BCBGames.Application.DTOs;
using BCBGames.Application.Queries.GetTransaction;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BCBGames.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("deposit")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deposit(
        [FromBody] DepositRequest request,
        CancellationToken ct)
    {
        var idempotencyKey = GetIdempotencyKey();
        var command = new DepositCommand(request.AccountId, request.Amount, request.Description, idempotencyKey);
        var transaction = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
    }

    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Withdraw(
        [FromBody] WithdrawRequest request,
        CancellationToken ct)
    {
        var idempotencyKey = GetIdempotencyKey();
        var command = new WithdrawCommand(request.AccountId, request.Amount, request.Description, idempotencyKey);
        var transaction = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
    }

    [HttpPost("purchase")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Purchase(
        [FromBody] PurchaseRequest request,
        CancellationToken ct)
    {
        var idempotencyKey = GetIdempotencyKey();
        var command = new PurchaseCommand(request.AccountId, request.Amount, request.Merchant, idempotencyKey);
        var transaction = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
    }

    private string? GetIdempotencyKey()
    {
        if (Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey))
            return idempotencyKey.ToString();

        if (Request.Headers.TryGetValue("X-Request-Id", out var requestId))
            return requestId.ToString();

        return null;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid id, CancellationToken ct)
    {
        var query = new GetTransactionQuery(id);
        var transaction = await _mediator.Send(query, ct);
        return transaction is null ? NotFound() : Ok(transaction);
    }
}
