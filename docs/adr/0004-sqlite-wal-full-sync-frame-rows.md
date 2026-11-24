# 0004 — SQLite WAL com `synchronous=FULL` e uma linha por instante

**Contexto.** A máquina perde energia sem aviso. O que foi lido até ~1 s antes precisa estar no disco. Uma linha por sensor por segundo daria 80–150 MB/dia e ~1 GB/dia de escrita.

**Decisão.**
- `journal_mode=WAL` e `synchronous=FULL`: cada commit (1 por segundo) é sincronizado no disco.
- Uma linha por instante (`FRAME`) com um vetor `float32` na ordem de um catálogo de sensores por segmento; um único `FrameCodec` conhece o formato.
- WAL persistente e ACL que dá ao painel leitura no banco e escrita só no `-shm`, para o modo somente leitura funcionar mesmo com o coletor parado.

**Alternativas.** EF Core (esconderia os PRAGMAs); arquivo binário próprio (reinventa durabilidade); uma linha por leitura (volume 5× maior).

**Consequências.** Consultar um sensor isolado exige decodificar quadros na aplicação. A garantia contra queda de energia vem do contrato do SQLite e não é testável sem hardware de injeção de falha; o que se testa é a consistência após matar o processo. O comportamento do modo somente leitura é validado no Plano 02; plano B: named pipe com ACL.
