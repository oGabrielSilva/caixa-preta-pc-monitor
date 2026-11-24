# 0001 — .NET 10 LTS

**Contexto.** O projeto é uma ferramenta de código aberto que deve ficar estável por anos. Em novembro de 2026 existem o .NET 10 (LTS, suporte até 14/11/2028) e o .NET 11 (STS, lançado em 10/11/2026). A biblioteca de sensores publica alvo `net10.0`.

**Decisão.** Todos os projetos usam `net10.0` (os do lado do coletor, `net10.0-windows`). O SDK é fixado em `global.json` (10.0.404, `rollForward: latestFeature`). Os pacotes Microsoft seguem a linha `10.0.x`.

**Alternativas.** .NET 11: recursos mais novos, mas é STS, e os pacotes ASP.NET 11 exigem `net11.0`.

**Consequências.** Atualizar para o próximo LTS (.NET 12) quando sair. Os testes usam Microsoft.Testing.Platform (`global.json` → `test.runner`), exigido pelo xUnit v3 no SDK 10.
