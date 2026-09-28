# PicPay Simplificado (.NET)

Port para **.NET 10** do meu [picpay-simplificado em TypeScript](https://github.com/Gstxxx/picpay-simplificado): uma API de transferências entre usuários comuns e lojistas, com autorização externa, idempotência, notificação assíncrona via outbox e testes de integração contra Postgres real.

O objetivo foi mostrar domínio da plataforma .NET usando as peças padrão do ecossistema, sem bibliotecas exóticas, e corrigir dois bugs que encontrei relendo a versão original.

## Stack

| Preocupação | Versão TS | Esta versão |
|---|---|---|
| HTTP | Hono + zod-validator | ASP.NET Core Minimal API + FluentValidation |
| Banco | Prisma + SQLite | EF Core 10 + PostgreSQL, migrations, snake_case |
| Débito atômico | `$transaction` + `updateMany` | `BeginTransactionAsync` + `ExecuteUpdateAsync` |
| Auth | `hono/jwt` | `AddJwtBearer`, bcrypt |
| Chamadas externas | `fetchWithResilience` (feito à mão) | `HttpClientFactory` + `AddStandardResilienceHandler` (Polly) |
| Worker | `setInterval` | `BackgroundService` + `FOR UPDATE SKIP LOCKED` |
| Rate limit | middleware próprio | `AddRateLimiter` nativo |
| Health | `/healthz`, `/readyz` | `AddHealthChecks().AddDbContextCheck()` |
| Config | zod | Options pattern + `ValidateDataAnnotations().ValidateOnStart()` |
| Testes | vitest + nock + supertest | xUnit + `WebApplicationFactory` + Testcontainers + WireMock.Net |
| Extras | — | Swagger, Serilog, Docker Compose, GitHub Actions |

## Rodando

Pré-requisitos: .NET SDK 10 e Docker.

```bash
docker compose --profile app up --build
```

A API sobe em `http://localhost:8080` com o Swagger em `/swagger`. As migrations são aplicadas no start e três usuários de exemplo são criados (senha `senha1234`):

| Email | Tipo | Saldo |
|---|---|---|
| alice@picpay.dev | Comum | R$ 1.000,00 |
| bruno@picpay.dev | Comum | R$ 250,00 |
| loja@picpay.dev | Lojista | R$ 0,00 |

Para desenvolver com hot reload, suba só o banco e rode a API localmente:

```bash
docker compose up -d postgres
dotnet run --project src/PicPay.Api
```

Testes (precisa do Docker rodando, o Testcontainers sobe um Postgres descartável):

```bash
dotnet test
```

## Endpoints

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| POST | `/auth/register` | — | Cria usuário (`Common` com CPF, `Merchant` com CNPJ) |
| POST | `/auth/login` | — | Retorna o JWT |
| GET | `/users/me` | JWT | Dados e saldo do usuário logado |
| POST | `/transfer` | JWT | Transfere do usuário logado para `payee`. Aceita `Idempotency-Key` |
| GET | `/transfer/{id}` | JWT | Consulta uma transferência (só pagador ou recebedor) |
| GET | `/healthz` | — | Liveness |
| GET | `/readyz` | — | Readiness (checa o Postgres) |

```http
POST /transfer
Authorization: Bearer <token>
Idempotency-Key: 7f1c2e9a-pedido-42
Content-Type: application/json

{ "value": 100.00, "payee": "01a0e99d-c2b5-7bb0-ae9c-0879d77bf5a5" }
```

| Status | Quando |
|---|---|
| 201 | Transferência criada |
| 200 + `Idempotent-Replayed: true` | Mesma `Idempotency-Key` repetida: devolve a transferência original |
| 400 | Payload inválido ou transferência para si mesmo |
| 403 | Lojista tentando enviar, `payer` diferente do token ou autorizador negou |
| 404 | Recebedor não existe |
| 422 | Saldo insuficiente ou `Idempotency-Key` reusada com outro payload |
| 429 | Rate limit |
| 503 | Autorizador fora do ar (após retries) |

Erros seguem o formato `application/problem+json` (RFC 9457) com um campo `code` estável, por exemplo `transfer.insufficient_funds`.

## Arquitetura

```
src/
├─ PicPay.Domain/          entidades e regras (User, Transaction, NotificationOutbox, RetrySchedule)
├─ PicPay.Application/     casos de uso (AuthService, TransferService), validators, portas (interfaces)
├─ PicPay.Infrastructure/  EF Core, repositórios, clientes HTTP com Polly, JWT, bcrypt, worker da outbox
└─ PicPay.Api/             endpoints, auth, rate limit, Swagger, health checks, composição
tests/
├─ PicPay.UnitTests/         domínio e validators
└─ PicPay.IntegrationTests/  API real + Postgres (Testcontainers) + autorizador/notificador falsos (WireMock)
```

As dependências apontam para dentro: `Api → Infrastructure → Application → Domain`. A camada de aplicação não conhece EF Core nem HTTP. Ela depende de interfaces como `IUserRepository`, `IPaymentAuthorizer` e `IUnitOfWork`, e a infraestrutura as implementa. Os casos de uso retornam `Result<T>` em vez de lançar exceções para erros de negócio, e a API traduz cada `ErrorType` para um status HTTP num único lugar (`ErrorResults.ToProblem`).

### Fluxo da transferência

1. Rejeita se `payer` veio no corpo e é diferente do `sub` do token.
2. Se há `Idempotency-Key` e ela já existe para esse pagador, devolve a transferência original.
3. Valida pagador (existe, não é lojista, saldo aparente suficiente) e recebedor.
4. Consulta o autorizador externo **fora** da transação do banco, para não segurar locks durante uma chamada de rede.
5. Abre a transação e, nesta ordem:
   - insere a `transaction` (o índice único `(payer_id, idempotency_key)` serializa requisições concorrentes);
   - debita com `UPDATE users SET balance = balance - @v WHERE id = @payer AND balance >= @v` e aborta se nenhuma linha foi afetada;
   - credita o recebedor;
   - grava a mensagem na `notification_outbox`;
   - faz o commit.
6. O `OutboxWorker` envia a notificação depois, com retry e backoff exponencial. Se o notificador cair, a transferência já está confirmada e a mensagem fica pendente até ser entregue.

A checagem de saldo do passo 3 serve só para não incomodar o autorizador à toa. Quem garante que o saldo nunca fica negativo é o `UPDATE` condicional, reforçado por uma `CHECK (balance >= 0)` no banco. O teste `Concurrent_transfers_never_overdraw` dispara 10 transferências de R$ 30 em paralelo contra um saldo de R$ 100 e verifica que exatamente 3 passam.

### Resiliência

O autorizador e o notificador usam `AddStandardResilienceHandler`, que combina timeout por tentativa, retry exponencial com jitter (só para 5xx, 408, 429 e falhas de rede), circuit breaker e timeout total. Um 403 do autorizador é uma resposta de negócio e não é repetido. Timeout, circuito aberto ou 5xx persistente viram `503` para o cliente, sem mexer em saldo.

O notificador tem duas camadas: o retry curto do Polly para falhas momentâneas e o backoff da outbox (5s, 10s, 20s... até 10 min, no máximo 8 tentativas) para indisponibilidades longas. Depois da última tentativa a mensagem fica como `Failed`, com o último erro gravado.

O worker busca o lote com `FOR UPDATE SKIP LOCKED`, então várias réplicas da API podem rodar ao mesmo tempo sem enviar a mesma notificação duas vezes. O custo é manter os locks do lote durante o envio. Com lotes pequenos e timeout curto isso é aceitável aqui. Em volume maior, eu trocaria por "reservar" as linhas com um `UPDATE ... RETURNING` e enviar fora da transação.

## Bugs corrigidos em relação à versão TS

### 1. Qualquer usuário logado conseguia transferir dinheiro de outra pessoa

Na versão original, o `payer` vinha do corpo da requisição e nunca era comparado com o `sub` do JWT. Bastava estar logado e mandar o id de outra pessoa como `payer`.

Aqui o pagador **sempre** sai do token. O campo `payer` continua aceito no corpo por compatibilidade com o enunciado do desafio, mas se for diferente do usuário autenticado a resposta é `403 transfer.payer_mismatch`, sem nenhuma consulta ao saldo.

Teste: `Payer_in_body_must_match_the_token`.

### 2. Requisições simultâneas com a mesma Idempotency-Key geravam erro 500

O código original consultava a chave e só depois inseria a transação. Se duas requisições passassem pela consulta ao mesmo tempo, a constraint `unique` barrava a segunda com uma exceção não tratada, ou seja, 500. Para o cliente, que está justamente repetindo a chamada por segurança, isso é o pior resultado possível: ele não sabe se o dinheiro saiu.

A correção tem duas partes:

- A transação é **inserida antes do débito**. A segunda requisição fica bloqueada no índice único até a primeira fazer commit e então recebe a violação, sem ter tocado no saldo. Se a primeira der rollback (por falta de saldo, por exemplo), a segunda segue normalmente.
- A violação de unicidade (`23505`) é traduzida para `UniqueConstraintException` na infraestrutura. O `TransferService` a captura, busca a transferência vencedora e devolve `200` com `Idempotent-Replayed: true`, igual a uma repetição sequencial.

A chave também passou a ser única **por pagador** e não global, e reusar a mesma chave com outro valor ou recebedor retorna `422`, em vez de devolver silenciosamente uma transferência diferente da que foi pedida.

Teste: `Concurrent_requests_with_same_idempotency_key_debit_once_and_never_fail` dispara 8 requisições em paralelo com a mesma chave (com atraso no autorizador para garantir a corrida) e verifica que todas retornam sucesso, só uma é `201`, todas têm o mesmo id e o saldo foi debitado uma única vez.

## Outras decisões

- **Guid v7** como chave primária: ordenável por tempo, bom para índice B-tree e não expõe contagem de registros.
- **`numeric(18,2)`** para valores monetários e `PrecisionScale` no validator, que rejeita `10.001` em vez de arredondar.
- **Login sem enumeração de usuários**: com email inexistente, o hasher ainda compara contra um hash fixo, e a resposta leva o mesmo tempo que uma senha errada.
- **Rate limit** por IP nas rotas de auth (janela fixa) e por usuário na transferência (token bucket).
- **Config validada no boot**: segredo JWT curto, URL inválida ou intervalo fora da faixa derrubam a aplicação na inicialização, e não na primeira requisição.
- **Testes de integração sem mocks de banco**: cada execução sobe um Postgres real, porque os bugs que importam aqui (concorrência, constraint, lock) não aparecem em banco em memória.
