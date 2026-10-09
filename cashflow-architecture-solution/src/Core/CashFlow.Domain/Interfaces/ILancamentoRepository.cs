using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CashFlow.Domain.Entities;

namespace CashFlow.Domain.Interfaces
{
    public interface ILancamentoRepository
    {
        Task<IEnumerable<Lancamento>> ObterTodosAsync();
        Task<Lancamento?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Lancamento lancamento);
        Task AtualizarAsync(Lancamento lancamento);
        Task RemoverAsync(Guid id);
    }
}
