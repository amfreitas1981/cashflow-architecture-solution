using CashFlow.Application.Commands;
using CashFlow.Application.Events;
using CashFlow.Application.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Interfaces;
using MediatR;

namespace CashFlow.Application.Handlers;

public class CriarLancamentoCommandHandler : IRequestHandler<CriarLancamentoCommand, Guid>
{
    private readonly ILancamentoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public CriarLancamentoCommandHandler(
        ILancamentoRepository repository, 
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(CriarLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = new Lancamento(
            request.ComercianteId,
            request.Valor,
            request.Tipo,
            request.Descricao
        );

        await _repository.AdicionarAsync(lancamento, cancellationToken);
        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        var evento = new LancamentoCriadoEvent(
            lancamento.Id,
            lancamento.ComercianteId,
            lancamento.Valor,
            lancamento.Tipo,
            lancamento.DataHora
        );

        await _eventPublisher.PublishAsync(evento, cancellationToken);

        return lancamento.Id;
    }
}