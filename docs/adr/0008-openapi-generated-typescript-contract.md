# 0008 — Contrato TypeScript gerado do OpenAPI

**Contexto.** Definir os mesmos DTOs em C# e TypeScript duplica código e deixa os dois lados divergirem.

**Decisão.** Os DTOs são records C#. O build gera `openapi/PcMonitor.Dashboard.json` (Microsoft.AspNetCore.OpenApi + Microsoft.Extensions.ApiDescription.Server); `pnpm gen:api` gera `app/api/schema.d.ts` (openapi-typescript); o app usa um único cliente `openapi-fetch`. Os dois arquivos gerados são versionados e o CI falha se regenerá-los produzir diferença.

**Alternativas.** Tipos TypeScript à mão; NSwag/Kiota (geram clientes inteiros, mais código para revisar).

**Consequências.** Toda mudança de API exige `dotnet build` + `pnpm gen:api` antes do commit. Os comentários XML dos records viram documentação no OpenAPI e no TypeScript.
