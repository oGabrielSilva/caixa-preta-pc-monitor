# 0003 — Coletor e painel separados, privilégio mínimo

**Contexto.** A coleta precisa começar no boot, sem login, e precisa de privilégio de administrador. O painel é uma interface web que não deveria rodar com esse privilégio.

**Decisão.** Dois serviços Windows: `PcMonitor.Collector` (LocalSystem) lê e grava; `PcMonitor.Dashboard` (conta virtual `NT SERVICE\PcMonitorDashboard`) abre o banco em somente leitura e serve a API e o painel **apenas em `127.0.0.1`** (porta configurável, endereço não). Eles não se comunicam; o banco é o contrato.

**Alternativas.** Processo único como administrador (coleta só após login, interface privilegiada); serviço + app desktop via named pipe (protocolo próprio para manter).

**Consequências.** Permissões de arquivo precisam ser desenhadas (ver ADR 0004). O endereço de escuta é fixado em código: `ASPNETCORE_URLS` ou `--urls` não conseguem expor o painel na rede.
