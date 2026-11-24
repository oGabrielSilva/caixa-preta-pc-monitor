# 0014 — API de leitura, ao vivo por SSE e privacidade dos eventos

**Contexto.** O painel precisa mostrar leituras a cada segundo, as quedas com o diagnóstico e o histórico, lendo o banco que o coletor grava ([ADR 0003](0003-collector-dashboard-split-least-privilege.md), [ADR 0004](0004-sqlite-wal-full-sync-frame-rows.md)). Os dois processos não conversam. Os dados de alguns eventos do Windows (1074) trazem o nome do computador e do usuário.

**Decisão.**
- **REST somente leitura** (`/api/live`, `/api/incidents`, `/api/incidents/{id}`, `/api/history/sensors`, `/api/history`) sobre portas de leitura da Application (`ILiveReader`, `IIncidentQuery`, `IHistoryReader`, `IStorageStatus`) implementadas no SQLite com `OpenReader()`.
- **Ao vivo por Server-Sent Events** (`/api/live/stream`): o painel consulta o banco a cada 1 s (`LiveCursor`/`LiveFeed`) e envia `frames`, `segment` (o cliente recarrega o retrato) e `state` a cada 5 s, que também serve de batimento. Um único tipo de evento no contrato (`LiveEvent`). Sem canal entre os processos: o banco continua sendo o contrato.
- **Portão do armazenamento:** os endpoints de dados respondem 503 com ProblemDetails (`storage.missing`, `storage.schema_too_old`, `storage.schema_too_new`, `storage.unreadable`) enquanto o banco não puder ser lido; `/api/health` responde sempre e informa o estado.
- **Privacidade por construção:** a API nunca devolve `data_json` de incidente nem de evento. Eventos saem só com provedor, id, horário e nível; o tipo `RelatedEventView` da Application nem tem campo de dados.
- **Conta e permissões:** o serviço se chama `PcMonitor.Dashboard`, e a conta virtual tem o nome do serviço: `NT SERVICE\PcMonitor.Dashboard` (o ADR 0003 citava `PcMonitorDashboard`; vale este nome). A conta recebe só **leitura** em `%ProgramData%\PcMonitor`. O leitor abre com o `-shm` também só leitura, o que funciona porque o WAL é persistente; se a verificação na máquina mostrar o contrário, o plano B do estudo é dar escrita só no `-shm`.
- **Números e enums no JSON:** números estritos (`JsonNumberHandling.Strict`, para o TypeScript gerado não virar `number | string`), enums por nome, leituras sem valor como `null`.

**Alternativas.** WebSocket (bidirecional sem necessidade); polling HTTP pelo navegador (mais requisições, mesma consulta ao banco); notificação do coletor por named pipe (canal novo entre os processos, que o ADR 0003 evita).

**Consequências.** Cada aba aberta faz uma consulta leve por segundo ao banco. Uma leitura que falhar (banco ocupado) é registrada e tentada de novo no ciclo seguinte, sem fechar o fluxo. O painel instalado antes do coletor funciona e mostra o aviso até o banco existir.
