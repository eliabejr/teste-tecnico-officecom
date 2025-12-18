using BCBGames.Application.DTOs;
using MediatR;

namespace BCBGames.Application.Queries.GetStatement;

public record GetStatementQuery(
    Guid AccountId,
    int Page = 1,
    int PageSize = 50) : IRequest<StatementResponse?>;
