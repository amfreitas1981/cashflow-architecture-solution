using CashFlow.Domain.Entities;
using CashFlow.Domain.Interfaces;
using CashFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Repositories;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly CashFlowDbContext _context;

    public LancamentoRepository(CashFlowDbContext context)
    {
        _context = context;
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken)
    {
        await _context.Lancamentos.AddAsync(lancamento, cancellationToken);
    }

    public async Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }
}