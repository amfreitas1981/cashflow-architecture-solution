# Sistema de Controle de Fluxo de Caixa e Consolidado Diário

Solução enterprise resiliente e de alta performance desenvolvida em **.NET 8 (C#)**, baseada nos princípios de **Domain-Driven Design (DDD)**, **Clean Architecture**, **CQRS** e **Event-Driven Architecture (EDA)**.

---

## 📋 Índice

1. [Visão Geral da Solução](#-visão-geral-da-solução)
2. [Arquitetura e Tecnologias](#-arquitetura-e-tecnologias)
3. [Estrutura do Projeto](#-estrutura-do-projeto)
4. [Componentes da Solução e Responsabilidades](#-componentes-da-solução-e-responsabilidades)
   - [Domain: Entidade e Enum](#1-domain-entidade-e-enum)
   - [Application: Command e DTO](#2-application-command-e-dto)
   - [Application: Command Handler](#3-application-command-handler)
   - [Infrastructure: Repository (EF Core)](#4-infrastructure-repository-ef-core)
   - [Infrastructure: Event Publisher (RabbitMQ)](#5-infrastructure-event-publisher-rabbitmq)
   - [API: Controller](#6-api-controller)
   - [Worker: Consumidor de Consolidado](#7-worker-consumidor-de-consolidado)
5. [Como Executar o Projeto](#-como-executar-o-projeto)
6. [Estratégia de Testes](#-estratégia-de-testes)

---

## 🏽 Visão Geral da Solução

O sistema atende a dois grandes requisitos de negócio:
1. **Serviço de Lançamentos (Core Domain):** Processamento síncrono e imutável de entradas (créditos) e saídas (débitos) financeiros com resposta ultra-rápida.
2. **Serviço de Consolidado Diário (Supporting Domain):** Processamento assíncrono acionado por eventos (`LancamentoCriadoEvent`) para consolidar e manter o saldo atualizado por comerciante em cache (**Redis**).

---

## 🛠 Arquitetura e Tecnologias

- **Linguagem Principal:** .NET 8 (C#)
- **Framework Web:** ASP.NET Core Web API
- **Mensageria:** RabbitMQ (via MassTransit ou cliente AMQP nativo)
- **Persistência de Dados:** PostgreSQL com Entity Framework Core 8
- **Cache de Leitura:** Redis Cluster
- **MediatR:** Padrão Mediator para desacoplamento de CQRS

---

## 📁 Estrutura do Projeto

```text
cashflow-architecture-solution/
├── README.md
├── .gitignore
├── docs/
│   ├── guia_executivo_arquitetura_fluxo_de_caixa.docx
│   ├── guia_executivo_arquitetura_fluxo_de_caixa.md
│   ├── [APRESENTAÇÃO] - Arquitetura de Solução - Fluxo de Caixa.pptx
│   └── architecture/
│       └── images/
│           ├── C4-Model-Verity-Nivel-1.png
│           ├── C4-Model-Verity-Nivel-2.png
│           └── C4-Model-Verity-Nivel-3.png
│
├── infra/
│   └── docker/
│       └── docker-compose.yml
│
├── src/
│   ├── Core/
│   │   ├── CashFlow.Domain/
│   │   │   ├── Entities/
│   │   │   │   └── Lancamento.cs
│   │   │   ├── Enums/
│   │   │   │   └── TipoLancamento.cs
│   │   │   └── Interfaces/
│   │   │       └── ILancamentoRepository.cs
│   │   └── CashFlow.Application/
│   │       ├── Events/
│   │       │   └── LancamentoCriadoEvent.cs
│   │       ├── Commands/
│   │       │   ├── CriarLancamentoCommand.cs
│   │       │   └── CriarLancamentoCommandHandler.cs
│   │       ├── DTOs/
│   │       │   └── LancamentoResponseDto.cs
│   │       └── Interfaces/
│   │           └── IEventPublisher.cs
│   ├── Infrastructure/
│   │   └── CashFlow.Infrastructure/
│   │       ├── Persistence/
│   │       │   ├── CashFlowDbContext.cs
│   │       │   └── Repositories/
│   │       │       └── LancamentoRepository.cs
│   │       └── Messaging/
│   │           └── RabbitMQPublisher.cs
│   └── Presentation/
│        ├── CashFlow.API/
│        │   ├── Controllers/
│        │   │   └── LancamentosController.cs
│        │   └── Program.cs
│        │
│        └── CashFlow.ConsolidadoWorker/
│            ├── Consumers/
│            │   └── LancamentoCriadoConsumer.cs
│            └── Program.cs
│
└── tests/
```

---

## 💻 Componentes da Solução e Responsabilidades

Nesta seção é detalhado o papel de cada arquivo na arquitetura e o fluxo funcional que ele executa dentro da aplicação.

### 1. Domain: Entidade e Enum

- **`TipoLancamento.cs` (Enum):**
  Define a classificação dos lançamentos financeiros no sistema (`Credito = 1` ou `Debito = 2`), garantindo consistência de domínio sem uso de strings mágicas.

- **`Lancamento.cs` (Entidade de Domínio):**
  Representa a raiz de agregação do lançamento financeiro. Responsável por encapsular as regras de negócio essenciais e garantir o estado invariante do objeto (ex.: validação de valor maior que zero e obrigatoriedade do identificador do comerciante). Possui setters privados para garantir imutabilidade dos dados após a criação.

---

### 2. Application: Command e DTO

- **`CriarLancamentoCommand.cs` (Command):**
  Objeto imutável (record) que representa a intenção do usuário/sistema de registrar uma nova transação. Implementa as interfaces do **MediatR** (`IRequest<Guid>`), carregando os dados necessários para o processamento da criação (Comerciante, Valor, Tipo e Descrição).

- **`LancamentoCriadoEvent.cs` (Evento de Domínio):**
  Estrutura de dados imutável enviada para a fila de mensageria assim que uma transação é efetuada com sucesso. Notifica os demais microsserviços/workers sobre a ocorrência da transação financeira com os dados relevantes para consolidação.

---

### 3. Application: Command Handler

- **`CriarLancamentoCommandHandler.cs` (Handler / Casos de Uso):**
  Orquestrador da regra de negócio de criação de lançamento. Implementa a interface `IRequestHandler` do **MediatR**:
  1. Instancia a entidade de domínio `Lancamento` (disparando as validações do modelo).
  2. Persiste a entidade no banco PostgreSQL utilizando o repositório (`ILancamentoRepository`) e confirma a transação (`UnitOfWork`).
  3. Monta e publica o evento de integração `LancamentoCriadoEvent` via mensageria para processamento assíncrono.
  4. Retorna o identificador gerado (`Guid`) para o chamador.

---

### 4. Infrastructure: Repository (EF Core)

- **`LancamentoRepository.cs` (Repositório de Persistência):**
  Implementa a interface `ILancamentoRepository` abstrata declarada na camada de Domínio. Utiliza o Entity Framework Core (`CashFlowDbContext`) para realizar as operações de leitura e gravação no banco de dados **PostgreSQL**, mantendo o acoplamento com o ORM restrito à camada de Infraestrutura.

---

### 5. Infrastructure: Event Publisher (RabbitMQ)

- **`RabbitMQPublisher.cs` (Publicador de Eventos):**
  Conecta-se ao broker **RabbitMQ** e publica os eventos de integração assíncronos na fila `lancamentos_queue`. Responsável por serializar a mensagem no formato JSON, configurar filas duráveis e assegurar a entrega da mensagem com baixa latência e desacoplamento do fluxo síncrono HTTP.

---

### 6. API: Controller

- **`LancamentosController.cs` (Ponto de Entrada HTTP):**
  Endpoint Web API (`POST /api/v1/lancamentos`) responsável por receber as requisições externas de criação de lançamentos. Delegada toda a lógica para o Mediator via `_mediator.Send(command)`, retornando respostas HTTP padronizadas (Status `201 Created` ou `400 BadRequest`).

---

### 7. Worker: Consumidor de Consolidado

- **`LancamentoCriadoConsumer.cs` (Worker Service Assíncrono):**
  Serviço executado em segundo plano (`BackgroundService`) que escuta continuamente a fila `lancamentos_queue` no **RabbitMQ**:
  1. Consome os eventos `LancamentoCriadoEvent` enviados pela API.
  2. Ajusta o sinal do valor (crédito é somado, débito é subtraído).
  3. Atualiza o saldo consolidado do comerciante no dia no **Redis** de maneira atômica utilizando o comando `StringIncrementAsync`.
  4. Confirma o recebimento da mensagem no broker (**BasicAck**).

---

## 🚀 Como Executar o Projeto

### Pré-requisitos
- .NET 8 SDK
- Docker / Docker Compose

### Executando Infraestrutura Via Docker Compose

Utilize o arquivo `infra/docker/docker-compose.yml` para subir a infraestrutura local:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: cashflow_postgres
    environment:
      POSTGRES_DB: cashflow_db
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: adminpassword
    ports:
      - "5432:5432"

  rabbitmq:
    image: rabbitmq:3-management
    container_name: cashflow_rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"

  redis:
    image: redis:alpine
    container_name: cashflow_redis
    ports:
      - "6379:6379"
```

Inicie os serviços:
```bash
docker-compose up -d
```

### Executando as Aplicações

1. **Iniciar a API:**
```bash
cd src/Presentation/CashFlow.API
dotnet run
```

2. **Iniciar o Worker Consolidador:**
```bash
cd src/Presentation/CashFlow.ConsolidadoWorker
dotnet run
```

---

## 🧪 Estratégia de Testes

- **Testes Unitários:** Executados na camada `Application` e `Domain` utilizando `xUnit` e `Moq`.
- **Testes de Integração:** Utilizando `Testcontainers` com instâncias reais de PostgreSQL e Redis durante a execução no pipeline de CI/CD.
- **Teste de Carga:** Roteiros de teste via `k6` validando o SLA de latência p99 $< 100\text{ms}$ no endpoint de Lançamentos.
