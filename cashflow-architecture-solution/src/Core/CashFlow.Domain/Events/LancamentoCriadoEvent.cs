using CashFlow.Domain.Enums;

namespace CashFlow.Application.Events;

public record LancamentoCriadoEvent(
    Guid LancamentoId,
    Guid ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    DateTime DataHora
);