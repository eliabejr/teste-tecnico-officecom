using BCBGames.Application.DTOs;
using BCBGames.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BCBGames.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    
    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request, 
        CancellationToken ct)
    {
        var account = await _accountService.CreateAccountAsync(request, ct);
        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
    }
    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(Guid id, CancellationToken ct)
    {
        var account = await _accountService.GetAccountAsync(id, ct);
        return account is null ? NotFound() : Ok(account);
    }
    
    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken ct)
    {
        var balance = await _accountService.GetBalanceAsync(id, ct);
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
        
        var statement = await _accountService.GetStatementAsync(id, page, pageSize, ct);
        return statement is null ? NotFound() : Ok(statement);
    }
}
