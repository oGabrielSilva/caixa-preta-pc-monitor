# Arquitetura

Documentação viva da arquitetura. Cada diagrama existe **uma vez só**, aqui; ADRs e AGENTS.md apontam para estas páginas em vez de copiá-las.

| Página | Conteúdo |
|---|---|
| [overview.md](overview.md) | Contexto, contêineres (processos: coletor, dashboard, app da bandeja) e camadas |
| [flows.md](flows.md) | Ciclo do coletor, detecção de incidentes, diagnóstico, ao vivo e painel fixo |
| [data-model.md](data-model.md) | Modelo de dados do SQLite |
| [frontend.md](frontend.md) | Estrutura do painel, contrato OpenAPI → TypeScript, exportação e a landing page |

Decisões e seus porquês: [ADRs](../adr/README.md). Contexto original e alternativas avaliadas: [estudo de arquitetura](../estudos/2026-11-24-estudo-arquitetura.md) (documento histórico, não é atualizado).

**Legenda de status:** ✅ implementado · 🕓 planejado (o plano indicado entrega).
