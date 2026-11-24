# 0009 — AGENTS.md como fonte única para agentes

**Contexto.** Agentes de IA (Claude Code, Codex e outros) leem instruções de arquivos diferentes. Manter o mesmo texto em `AGENTS.md` e `CLAUDE.md` duplicaria conteúdo.

**Decisão.** As instruções ficam em `AGENTS.md`: na raiz e em cada projeto de `src/` e `tests/`. Cada pasta com `AGENTS.md` tem um `CLAUDE.md` com exatamente `@AGENTS.md` (import do Claude Code). `scripts/Test-AgentDocs.ps1` verifica a convenção no CI.

**Alternativas.** Só `CLAUDE.md` (exclui outros agentes); conteúdo duplicado (diverge com o tempo).

**Consequências.** Ao criar um projeto, crie os dois arquivos; o CI falha caso contrário.
