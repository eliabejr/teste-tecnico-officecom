using BCBGames.Application.Commands.CreateAccount;
using BCBGames.Application.DTOs;
using BCBGames.Application.Queries.GetAccount;
using BCBGames.Application.Queries.GetBalance;
using BCBGames.Application.Queries.GetStatement;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BCBGames.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken ct)
    {
        var command = new CreateAccountCommand(request.OwnerName, request.InitialBalance);
        var account = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(Guid id, CancellationToken ct)
    {
        var query = new GetAccountQuery(id);
        var account = await _mediator.Send(query, ct);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken ct)
    {
        var query = new GetBalanceQuery(id);
        var balance = await _mediator.Send(query, ct);
        return balance is null ? NotFound() : Ok(balance);
    }

    [HttpGet("{id:guid}/statement")]
    [ProducesResponseType(typeof(StatementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatement(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = new GetStatementQuery(id, page, pageSize);
        var statement = await _mediator.Send(query, ct);
        return statement is null ? NotFound() : Ok(statement);
    }
}
