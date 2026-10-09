using CashFlow.Domain.Enums;
using MediatR;

namespace CashFlow.Application.Commands;

public record CriarLancamentoCommand(
    Guid ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    string Descricao
) : IRequest<Guid>;