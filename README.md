# Partners API

API ASP.NET Core para gerir clientes e fornecedores (parceiros) de uma empresa. Cada entidade pode ser cliente, fornecedor ou ambos, e existe apenas um registo por NIF (`TaxNumber`).

## Versões utilizadas

| Componente | Versão |
|---|---|
| .NET SDK | 10.0.401 (`net10.0`, LTS) |
| ASP.NET Core / EF Core | 10.0.x (EF Core 10.0.12) |
| Base de dados | SQL Server 2025 Developer (17.0); compatível com SQL Server 2019+ e Express |
| Testes | xUnit 2.9, `Microsoft.AspNetCore.Mvc.Testing` |
| Ferramenta de migrações | `dotnet-ef` 10.0.12 (ferramenta local em `dotnet-tools.json`) |

## Estrutura

```
src/
  Partners.Domain          Entidade Partner, regras de validação e normalização do NIF (sem dependências)
  Partners.Application     Casos de uso (PartnerService), DTOs, critérios de pesquisa, contrato do repositório
  Partners.Infrastructure  EF Core: DbContext, configuração, migrações, dados de exemplo, repositório
  Partners.Api             Controller, tratamento global de erros (ProblemDetails), Program.cs
tests/
  Partners.Tests           Testes unitários do domínio e testes de integração da API contra SQL Server
```

As dependências apontam para dentro: `Api → Application/Infrastructure → Domain`. A Application define `IPartnerRepository` e a Infrastructure implementa-o, por isso as regras não dependem do EF Core.

## Preparação

1. Instalar o [.NET 10 SDK](https://dotnet.microsoft.com/download) e o SQL Server (Developer ou Express).
2. Configurar a connection string em `src/Partners.Api/appsettings.json` (por defeito usa a instância local com autenticação Windows):

   ```json
   "ConnectionStrings": {
     "PartnersDb": "Server=localhost;Database=PartnersDb;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

   Também pode ser definida pela variável de ambiente `ConnectionStrings__PartnersDb`. Para SQL Server Express use `Server=localhost\\SQLEXPRESS;...`.

3. Restaurar as ferramentas e criar a base de dados (aplica as migrações e os dados de exemplo):

   ```bash
   dotnet tool restore
   dotnet ef database update --project src/Partners.Infrastructure --startup-project src/Partners.Api
   ```

## Execução

```bash
dotnet run --project src/Partners.Api --launch-profile http
```

A API fica disponível em `http://localhost:5018`. O documento OpenAPI está em `http://localhost:5018/openapi/v1.json` (ambiente Development).

Exemplos HTTP prontos a usar estão em [`src/Partners.Api/Partners.Api.http`](src/Partners.Api/Partners.Api.http) (Visual Studio, Rider ou a extensão REST Client do VS Code).

## Testes

```bash
dotnet test
```

Os testes de integração arrancam a API em memória (`WebApplicationFactory`) e usam uma base de dados **separada**, `PartnersDb_Tests`, que é recriada a partir das migrações no início da execução e limpa antes de cada teste. A base de dados de desenvolvimento nunca é tocada. Para usar outro servidor:

```bash
# PowerShell
$env:PARTNERS_TEST_CONNECTION_STRING = "Server=...;Database=PartnersDb_Tests;..."
dotnet test
```

## Endpoints

| Método | Rota | Sucesso | Erros |
|---|---|---|---|
| `POST` | `/api/partners` | `201` + `Location` | `400`, `409` (NIF duplicado) |
| `GET` | `/api/partners/{id}` | `200` (também para inativos) | `404` |
| `GET` | `/api/partners?Search=&Type=&IncludeInactive=&Page=&PageSize=` | `200` com `items`, `totalCount`, `page`, `pageSize` | `400` |
| `PUT` | `/api/partners/{id}` | `200` com a nova `version` | `400`, `404`, `409` (NIF duplicado ou versão desatualizada) |

Exemplo de pedido de criação:

```json
{ "name": " Ana Silva ", "taxNumber": "001.234.567", "type": "CUSTOMER", "email": null, "creditLimit": 1500.50 }
```

O `PUT` exige todos os campos (`name`, `taxNumber`, `type`, `email` — pode ser `null` —, `creditLimit`, `isActive`, `version`). Desativar e reativar faz-se com `isActive`.

Todos os erros usam o formato `ProblemDetails` (`application/problem+json`). Os erros de validação incluem `errors` por campo. Nenhuma resposta expõe stack traces.

## Decisões técnicas

- **NIF único com concorrência:** índice `UNIQUE` (`UX_Partners_TaxNumber`) sobre o valor normalizado. Dois pedidos simultâneos não podem criar duplicados porque a garantia é do SQL Server; a violação (erros 2601/2627) é convertida em `409`. Não há locks em memória.
- **Normalização do NIF:** remove espaços, pontos e hífenes e exige exatamente 9 dígitos ASCII (`char.IsAsciiDigit`), guardado como `varchar(9)` para preservar zeros à esquerda. Dígitos Unicode (ex.: largura total) são rejeitados para não contornar o índice único.
- **Concorrência otimista:** coluna `rowversion` exposta como `version` em Base64. No `PUT`, a versão enviada pelo cliente é usada como valor original do token de concorrência, e o EF Core gera `UPDATE ... WHERE Id = @id AND RowVersion = @version`. Se nenhuma linha for afetada, devolve `409`. A comparação acontece no SQL Server, de forma atómica.
- **Atualizações atómicas:** a entidade valida todos os campos antes de alterar qualquer um; uma atualização inválida não altera o registo.
- **CreditLimit:** `decimal(18,2)`, entre 0 e 1 000 000. Valores com mais de duas casas decimais são rejeitados (`400`), nunca arredondados.
- **Datas:** `CreatedAt`/`UpdatedAt` em UTC, definidas pelo servidor através de `TimeProvider`; um value converter garante `DateTimeKind.Utc` ao ler da base de dados.
- **Listagem:** filtros combinados por `AND`, contagem antes da paginação e `ORDER BY Name, Id`, tudo executado no SQL Server (`IQueryable`). `Type=CUSTOMER` inclui `BOTH`; `Type=SUPPLIER` inclui `BOTH`; `Type=BOTH` só `BOTH`. O `Search` procura por ocorrência parcial no nome, sem distinguir maiúsculas (collation `Latin1_General_CI_AS` explícita), **ou** no NIF normalizado; o ramo do NIF só é usado quando o texto normalizado não fica vazio e contém apenas dígitos, por isso `Search=Ana` ou `Search=---` não fazem corresponder todos os NIF. Existe um índice `(Name, Id)` para a ordenação.
- **Validação do pedido:** `required` nos DTOs (campo em falta → `400`), enums como texto (`CUSTOMER`, `SUPPLIER`, `BOTH`; números rejeitados), `CHECK constraints` na base de dados como defesa adicional.

## Dados de exemplo

A migração inicial insere 4 parceiros (NIFs `500000001` a `500000004`, incluindo um inativo e um `BOTH`). Não colidem com os exemplos do enunciado (ex.: `001.234.567`).

## Cenários do enunciado e testes

| Cenário | Testes |
|---|---|
| 1. Normalização, NIF duplicado, NIF reservado após desativação | `CreatePartnerTests` |
| 2. Filtros por tipo (`BOTH` em todos) | `ListPartnersTests.TypeFilter_*` |
| 3. Desativar/reativar por `PUT` | `UpdatePartnerTests.DeactivateAndReactivate_*` |
| 4. Manter o próprio NIF / NIF de outra entidade | `UpdatePartnerTests.Update_KeepingOwnTaxNumber_*`, `Update_ToTaxNumberOfAnotherPartner_*` |
| 5. Pedidos simultâneos (criação e atualização) com contextos independentes | `ConcurrencyTests` |
| 6. `CreditLimit=10.999`, NIF com letras, `Page=0`, `Type` inválido, `Version` malformada, sem alterações | `CreatePartnerTests`, `ListPartnersTests.List_WithInvalidQuery_*`, `UpdatePartnerTests.Update_WithMalformedVersion_*` |
| 7. Filtros combinados, `TotalCount` antes da paginação, desempate por `Id`, `Search=Ana` | `ListPartnersTests` |
