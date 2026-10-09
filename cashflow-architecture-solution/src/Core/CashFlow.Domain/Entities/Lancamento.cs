using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Entities;

public class Lancamento
{
    public Guid Id { get; private set; }
    public Guid ComercianteId { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public string Descricao { get; private set; }
    public DateTime DataHora { get; private set; }

    // Construtor protegido para EF Core
    protected Lancamento() { }

    public Lancamento(Guid comercianteId, decimal valor, TipoLancamento tipo, string descricao)
    {
        if (comercianteId == Guid.Empty)
            throw new ArgumentException("ComercianteId é obrigatório.", nameof(comercianteId));
        
        if (valor <= 0)
            throw new ArgumentException("O valor do lançamento deve ser maior que zero.", nameof(valor));

        Id = Guid.NewGuid();
        ComercianteId = comercianteId;
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao ?? string.Empty;
        DataHora = DateTime.UtcNow;
    }
}