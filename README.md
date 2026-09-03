# MarTech Orders API

Backend de gestão de pedidos em .NET 10, organizado em Clean Architecture com CQRS.

O foco desta entrega é a qualidade das fronteiras entre camadas e a fidelidade das regras de negócio ao domínio — não a quantidade de endpoints.

## Sumário

- [Como rodar](#como-rodar)
- [Endpoints](#endpoints)
- [Arquitetura](#arquitetura)
- [Decisões técnicas](#decisões-técnicas)
- [Regras de negócio](#regras-de-negócio)
- [Testes](#testes)
- [Qualidade e observabilidade](#qualidade-e-observabilidade)
- [Limitações conhecidas](#limitações-conhecidas)

## Como rodar

### Local

Requer o SDK do .NET 10.

```bash
dotnet restore
dotnet run --project src/MarTech.Orders.Api
```

A API sobe em `http://localhost:5080`. As migrations são aplicadas automaticamente na inicialização e o arquivo `orders.db` é criado na pasta do projeto da API.

Documentação interativa (apenas fora de Production): `http://localhost:5080/scalar`
Documento OpenAPI: `http://localhost:5080/openapi/v1.json`
Health check: `http://localhost:5080/health`

### Docker

```bash
docker compose up --build
```

A API fica em `http://localhost:8080` e o banco SQLite é persistido no volume `orders-data`, montado em `/data`. A imagem roda como usuário não-root e expõe um `HEALTHCHECK` apontando para `/health`.

Para trocar a chave de assinatura do JWT sem editar arquivo:

```bash
JWT_SIGNING_KEY="uma-chave-com-pelo-menos-32-caracteres" docker compose up --build
```

### Testes

```bash
dotnet test
```

## Endpoints

| Método | Rota | Auth | Respostas |
| --- | --- | --- | --- |
| POST | `/auth/login` | não | 200, 400, 401 |
| POST | `/api/orders` | sim | 201, 400, 401 |
| GET | `/api/orders?page=1&pageSize=10` | sim | 200, 400, 401 |
| GET | `/api/orders/{id}` | sim | 200, 401, 404 |
| PATCH | `/api/orders/{id}/cancel` | sim | 204, 401, 404, 409 |

Credenciais fixas: `dev@martech.com` / `Senha@123`.

### Exemplo de uso

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@martech.com","password":"Senha@123"}' | jq -r .accessToken)

curl -X POST http://localhost:8080/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
        "customerId": "8f9b1a2c-3d4e-4f50-9a61-72b83c94d5e6",
        "items": [
          { "productName": "Teclado", "quantity": 2, "unitPrice": 149.90 },
          { "productName": "Mouse", "quantity": 1, "unitPrice": 89.50 }
        ]
      }'
```

Erros seguem `application/problem+json` (RFC 9457), com `traceId` para correlação com os logs.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Invalid order state transition",
  "status": 409,
  "detail": "Order 01a0... cannot be cancelled because its status is Cancelled.",
  "traceId": "00-e7d0962c129d7b74a7ebe502df5f4e6c-b94385ef8dbdaeb5-01"
}
```

## Arquitetura

```
MarTech.Orders.Api             controllers, tratamento de erro, OpenAPI, observabilidade
        |
MarTech.Orders.Infrastructure  EF Core + SQLite, JWT, repositorios, unit of work
        |
MarTech.Orders.Application     commands, queries, validators, behaviors, abstracoes
        |
MarTech.Orders.Domain          entidades, invariantes, domain events, excecoes
```

As dependências apontam para dentro. `Domain` não referencia nenhum pacote de infraestrutura — nem EF Core, nem MediatR, nem FluentValidation. As abstrações ficam nas camadas internas (`IOrderRepository` no domínio; `IOrderReadRepository`, `IUnitOfWork`, `IDateTimeProvider`, `IUserDirectory` e `IAccessTokenIssuer` na Application) e `Infrastructure` as implementa.

Essas regras não estão apenas escritas aqui: estão verificadas em `tests/MarTech.Orders.Architecture.Tests` com NetArchTest, e quebram o build se alguém inverter uma seta.

A organização é por feature, não por tipo técnico. Cada caso de uso tem sua pasta com command/query, validator e handler juntos:

```
Application/Orders/CreateOrder/
    CreateOrderCommand.cs
    CreateOrderCommandValidator.cs
    CreateOrderCommandHandler.cs
```

Adicionar um caso de uso significa criar uma pasta — não editar seis arquivos espalhados.

## Decisões técnicas

### Controllers em vez de Minimal API

Ambos atendem. Optei por Controllers por três motivos:

1. **Convenção no roteamento.** `[Route("api/orders")]` com `[Authorize]` no nível da classe deixa a política de autenticação explícita e herdada por todas as ações. Em Minimal API isso vira `RequireAuthorization()` por grupo, e esquecer um grupo é uma falha silenciosa de segurança.
2. **Documentação declarativa.** `[ProducesResponseType]` mantém o contrato OpenAPI junto da ação. Com Minimal API os mesmos metadados viram encadeamento de `Produces<T>()`, que polui o registro das rotas.
3. **Familiaridade para revisão e manutenção.** É o formato que qualquer pessoa do time abre e entende sem contexto adicional.

A troca é performance de roteamento: Minimal API tem overhead menor por request. Nesta escala isso não é decisivo, e o custo de migrar depois é baixo — os controllers não têm lógica, apenas traduzem HTTP para uma mensagem do MediatR.

Cada action tem no máximo três linhas: montar o comando, enviar pelo `ISender`, escolher o status code. Não há `if` de regra de negócio, acesso a `DbContext` ou tratamento de exceção nos controllers — isso é garantido por teste de arquitetura.

### Sem repositório genérico

Não existe `IRepository<T>`. Ele resolveria um problema que não temos e criaria dois que teríamos.

`IOrderRepository` expõe exatamente duas operações, ambas com intenção clara:

```csharp
void Add(Order order);
Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
```

`GetByIdAsync` carrega o agregado completo com os itens, porque cancelar um pedido exige o agregado íntegro. Um `IRepository<T>.GetByIdAsync` genérico não teria como saber disso — ou carregaria itens sempre (desperdício), ou nunca (bug), ou exporia `IQueryable<T>` e vazaria EF Core para dentro da Application.

O caminho de leitura é separado: `IOrderReadRepository` devolve DTOs prontos, sem tracking. É o ponto onde CQRS deixa de ser só nomenclatura de pastas — escrita e leitura têm modelos e necessidades diferentes.

Uma sutileza deliberada: `ListAsync` projeta direto para `OrderSummaryResponse` sem materializar agregados, mas `GetByIdAsync` carrega a entidade e reutiliza o mapeamento de `OrderMappings`. Na listagem o over-fetching custa caro e a projeção se paga; em um único pedido não custa, e reutilizar o mapeamento evita que duas definições da mesma resposta divirjam com o tempo.

### TotalAmount vive no domínio

`Order.RecalculateTotal()` é a única linha do sistema que sabe que o total de um pedido é a soma de `UnitPrice * Quantity`. Ela roda dentro do agregado, é privada, e é chamada sempre que a composição de itens muda. Nenhum handler, controller ou query soma preços.

O valor é persistido em coluna, não recalculado a cada leitura. É redundância consciente, por duas razões:

- **Ninguém consegue divergir do domínio.** A coluna só é escrita por `RecalculateTotal()`. Se o total fosse recalculado no read model, existiriam duas implementações da mesma regra, e elas divergiriam na primeira mudança (desconto, frete, imposto).
- **A listagem não precisa carregar itens.** Sem a coluna, paginar 10 pedidos exigiria trazer todos os itens de todos eles só para somar em memória.

Trade-off aceito: se alguém alterar `order_items` por fora da aplicação, o total fica velho. Isso se resolve com a regra de que o agregado é o único caminho de escrita — que é justamente o que o repositório impõe aqui.

### Dinheiro é armazenado em centavos

SQLite não tem tipo decimal. O EF Core mapeia `decimal` para `TEXT`, e a própria documentação avisa que ordenação e agregação nesse formato são incorretas — `"9.90"` ordena depois de `"10.00"` porque a comparação é textual.

`MoneyToCentsConverter` guarda valores monetários como `INTEGER` de centavos:

```csharp
amount => decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero)),
cents  => cents / 100m
```

Os valores ficam ordenáveis e somáveis no banco sem perda de precisão. A contrapartida é limitar a escala a duas casas decimais, então essa restrição é explícita e validada em duas camadas: `Money.Normalize` rejeita no domínio (`UnsupportedMonetaryPrecisionException`) e o `CreateOrderCommandValidator` rejeita na borda com uma mensagem legível.

### Datas voltam em UTC

Pelo mesmo motivo — SQLite guarda `DateTime` como texto — o `DateTimeKind` se perde no round-trip e a data lida de volta vem como `Unspecified`. Um pedido criado às 12:00 UTC voltava serializado como `"2026-03-15T12:00:00"`, sem o `Z`, e qualquer cliente interpretaria como horário local.

`UtcDateTimeConverter` normaliza na escrita e reafirma `DateTimeKind.Utc` na leitura. Há um teste de integração que verifica exatamente isso, porque é o tipo de bug que passa em teste unitário e aparece em produção.

### Validação em duas camadas, de propósito

FluentValidation valida a **forma da requisição** (campos obrigatórios, faixas, tamanho, escala decimal) e responde 400 com a lista de erros por campo. O domínio valida **invariantes** e lança exceção.

Não é duplicação: são responsabilidades distintas. O validator existe para dar uma mensagem útil ao cliente HTTP. As guardas do domínio existem para que `Order` seja impossível de construir em estado inválido — inclusive a partir de um teste, de um job ou de um consumidor de fila que nunca passou pelo pipeline do MediatR. Se o validator for removido por engano, o sistema continua correto; só piora a mensagem de erro.

### Exceções mapeadas para ProblemDetails

O tratamento é feito por uma cadeia de `IExceptionHandler`, não por middleware com `try/catch`:

| Exceção | Status |
| --- | --- |
| `ValidationException` | 400 com dicionário de erros por campo |
| `OrderNotFoundException` | 404 |
| `InvalidCredentialsException` | 401 |
| `OrderNotCancellableException`, `OrderNotConfirmableException` | 409 |
| `DomainException` (demais) | 400 |
| qualquer outra | 500, com log e sem vazar detalhes |

A distinção entre 400 e 409 é **semântica de domínio**, não de transporte: cancelar um pedido já cancelado não é uma requisição malformada, é um conflito de estado. Essa decisão só é possível porque o domínio lança exceções específicas em vez de uma exceção genérica com mensagem em string.

### Domain events

`Order` registra `OrderPlacedDomainEvent`, `OrderCancelledDomainEvent` e `OrderConfirmedDomainEvent`. O `UnitOfWork` coleta os eventos das entidades rastreadas, salva, e só então publica via `IPublisher` do MediatR.

Não há nenhum handler consumindo esses eventos hoje — e isso é intencional. O ponto de extensão está pronto: quando aparecer "notificar o cliente no cancelamento" ou "baixar estoque na confirmação", é uma classe nova, sem tocar no handler do caso de uso. Publicar depois do `SaveChanges` garante que nenhum efeito colateral dispare para uma transação que não persistiu.

### Detalhes menores

- **`Guid.CreateVersion7()`** para IDs. GUID v7 tem prefixo temporal, então os IDs são monotonicamente crescentes: o índice não fragmenta como aconteceria com v4 aleatório, e a ordenação por ID coincide com a ordem de criação.
- **PBKDF2 com 210 mil iterações** e comparação em tempo constante (`CryptographicOperations.FixedTimeEquals`) mesmo para o usuário fixo. A senha não fica em memória como texto puro; o hash é gerado no startup a partir da configuração. O teste dizia que "usuário fixo é suficiente", mas escrever comparação de senha com `==` seria treinar o hábito errado.
- **`ValidateOnStart()`** nas opções de JWT e do usuário semente: chave curta ou ausente derruba a aplicação no boot, não no primeiro login.
- **Central Package Management** (`Directory.Packages.props`): versões em um único lugar, impossível dois projetos divergirem.
- **`TreatWarningsAsErrors`** ligado em toda a solução. O build está limpo, sem supressões, exceto `CA1707` nos projetos de teste — a convenção `Metodo_Cenario_Resultado` usa underscores de propósito.

## Regras de negócio

| Regra | Onde vive | Teste |
| --- | --- | --- |
| Pedido tem no mínimo 1 item | `Order.Place` | `Place_WithoutItems_Throws` |
| `Quantity > 0` | `OrderItem.Create` | `Place_WithNonPositiveQuantity_Throws` |
| `UnitPrice > 0` | `OrderItem.Create` | `Place_WithNonPositiveUnitPrice_Throws` |
| Só `Pending` pode ser cancelado | `Order.Cancel` | `Cancel_WhenConfirmed_Throws` |
| `TotalAmount` calculado no domínio | `Order.RecalculateTotal` | `Place_SumsLineTotalsIntoTotalAmount` |

Todas são invariantes do agregado. Nenhuma está em handler, controller ou validator — os validators apenas antecipam a mensagem de erro.

## Testes

60 testes em quatro projetos:

| Projeto | O que cobre |
| --- | --- |
| `Domain.Tests` | invariantes do agregado, cálculo do total, transições de estado, eventos |
| `Application.Tests` | todos os handlers, com dependências substituídas, e o `ValidationBehavior` |
| `Architecture.Tests` | direção das dependências entre camadas e convenções de código |
| `Api.IntegrationTests` | fluxo HTTP completo com `WebApplicationFactory` e SQLite real |

Os testes de handler verificam **comportamento observável**, não implementação: além do retorno, checam que `SaveChangesAsync` foi chamado exatamente uma vez no caminho feliz e **nenhuma vez** quando uma regra é violada. Um handler que persistisse antes de validar passaria em um teste de retorno e falharia aqui.

Os testes de integração sobem a aplicação real — pipeline do MediatR, autenticação JWT, EF Core, migrations — contra um arquivo SQLite temporário por fixture, removido no dispose. Cobrem login válido e inválido, acesso sem token, criação e leitura de pedido, o round-trip de UTC e de valor monetário, duplo cancelamento retornando 409, e paginação.

O tempo é injetado via `IDateTimeProvider`, então nenhum teste depende de `DateTime.UtcNow` nem é sensível ao relógio da máquina.

## Qualidade e observabilidade

**Logging.** `LoggingBehavior` envolve todo command e query, registrando nome da requisição, payload estruturado e tempo de execução. Acima de 500 ms o nível sobe para `Warning`, o que transforma latência em algo consultável em vez de algo que se descobre por reclamação. Usa `[LoggerMessage]` (source generator) em vez de interpolação de string: sem alocação quando o nível está desligado e sem boxing dos parâmetros.

Logar o payload de *toda* requisição tem um efeito colateral óbvio: `LoginCommand` carrega a senha e `LoginResponse` carrega o token. Requisições que trafegam segredo implementam o marcador `ISensitiveRequest`, e para elas o behavior registra nome e duração, mas substitui os payloads por `[redacted]`. A checagem é resolvida uma vez por tipo fechado (`static readonly` no genérico), então não custa nada por request. Há um teste que falha se uma senha voltar a aparecer no log.

**Tracing e métricas.** OpenTelemetry com exportador para console, instrumentando ASP.NET Core, HttpClient e o `ActivitySource` do EF Core — as queries aparecem como spans filhos do request.

**SonarQube.** Configurado no `docker-compose.yml` sob o profile `quality`, para não pesar o `docker compose up` do dia a dia:

```bash
docker compose --profile quality up sonarqube
# gerar um token em http://localhost:9000
SONAR_TOKEN=<token> docker compose --profile quality run --rm sonar-scanner
```

O serviço `sonar-scanner` roda `dotnet-sonarscanner` com cobertura via `dotnet-coverage`.

**CI.** `.github/workflows/ci.yml` faz restore, build em Release, testes com relatório `.trx` publicado como artefato, e build da imagem Docker.

## Limitações conhecidas

Coisas que ficaram de fora conscientemente, com o motivo:

- **Migrations no startup.** Atende ao requisito, mas com múltiplas réplicas todas tentariam migrar ao mesmo tempo. Em produção isso vira um job de migração separado, executado antes do rollout.
- **Sem controle de concorrência otimista.** Dois cancelamentos simultâneos do mesmo pedido podem ambos ler `Pending` e um sobrescrever o outro. A correção seria um token de concorrência incrementado no `SaveChanges` — o SQLite não tem `rowversion` nativo, então seria uma coluna de versão gerenciada manualmente. Não implementei para não introduzir complexidade que o teste não pede, mas é o primeiro item da lista se isso fosse para produção.
- **`CustomerId` vem do corpo da requisição.** O usuário autenticado aqui é um desenvolvedor, não um cliente. Num sistema real o `CustomerId` viria do token ou de um contexto de cliente, e a listagem seria escopada a ele.
- **Sem paginação por cursor.** `OFFSET` degrada em páginas altas. Com o ID sendo GUID v7 (ordenável no tempo), migrar para keyset pagination seria direto.
- **Sem rate limiting nem refresh token.** Fora do escopo do teste.
- **`Order.Confirm` não tem endpoint.** O enum exigido pelo teste inclui `Confirmed`, então a transição está modelada e testada no agregado, mas não exposta — não havia endpoint pedido para ela. Expor é uma action de três linhas.

## Sobre as dependências

Duas observações de licenciamento que motivaram escolhas de versão:

- **MediatR** está fixado na **12.5.0**, a última versão sob Apache 2.0. As versões seguintes passaram a exigir licença comercial acima de um limite de faturamento. O teste pede MediatR explicitamente, então ele está aqui — mas na 12.5.0, gratuita sem ressalvas.
- **FluentAssertions** não é usada. A partir da 8.x ela exige licença paga para uso comercial. Os testes usam **Shouldly**, com API equivalente e licença permissiva.
