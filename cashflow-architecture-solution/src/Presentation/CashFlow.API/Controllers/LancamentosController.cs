using CashFlow.Domain.Entities;
using CashFlow.Infrastructure.Messaging;
using CashFlow.Infrastructure.Persistence;
using CashFlow.Lancamentos.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Lancamentos.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LancamentosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRabbitMQPublisher _publisher;
    private readonly ILogger<LancamentosController> _logger;

    public LancamentosController(
        AppDbContext context, 
        IRabbitMQPublisher publisher, 
        ILogger<LancamentosController> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LancamentoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarLancamento([FromBody] CriarLancamentoRequest request)
    {
        try
        {
            var lancamento = new Lancamento(
                request.ComercianteId, 
                request.Valor, 
                request.Tipo, 
                request.Descricao
            );

            _context.Lancamentos.Add(lancamento);
            await _context.SaveChangesAsync();

            // Publica evento assíncrono para a fila do Consolidado Diário
            _publisher.PublishLancamentoCriado(lancamento);

            var response = new LancamentoResponse(
                lancamento.Id,
                lancamento.ComercianteId,
                lancamento.Valor,
                lancamento.Tipo.ToString(),
                lancamento.Descricao,
                lancamento.DataCriacao
            );

            return CreatedAtAction(nameof(CriarLancamento), new { id = lancamento.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Erro de validação ao criar lançamento.");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao processar lançamento.");
            return StatusCode(500, new { error = "Erro interno no servidor." });
        }
    }
}
