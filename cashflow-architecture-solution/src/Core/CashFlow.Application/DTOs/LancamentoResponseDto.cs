using CashFlow.Domain.Entities;

namespace CashFlow.Lancamentos.Api.DTOs;

public record CriarLancamentoRequest(
    Guid ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    string Descricao
);

public record LancamentoResponseDto(
    Guid Id,
    Guid ComercianteId,
    decimal Valor,
    string Tipo,
    string Descricao,
    DateTime DataCriacao
);