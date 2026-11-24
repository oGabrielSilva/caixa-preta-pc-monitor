# 0012 — Diagnóstico explicável, versionado e gravado por incidente

**Contexto.** O dono precisa entender *por que* o PC Monitor aponta uma causa, e a ferramenta é aberta: outras pessoas vão ajustar regras. Diagnósticos opacos ou com falsa precisão minariam a confiança, e textos gravados no banco amarrariam o idioma.

**Decisão.**
- O diagnóstico é **lógica pura no Domain**: regras independentes (`IDiagnosticRule`) devolvem `Finding`s (evidência a favor/contra uma hipótese com força 1–3, observação ou limitação); o `DiagnosisEngine` só soma, ordena e atribui confiança. Nova regra = nova classe listada em `DiagnosticRules.All` (um teste falha se faltar).
- Findings são **códigos com parâmetros**, nunca texto; o painel traduz. O catálogo está em [diagnosis.md](../architecture/diagnosis.md).
- Cada incidente guarda **um** diagnóstico com a versão do motor; quando regras ou limiares mudam, `DiagnosisEngine.Version` sobe e todos os incidentes são reavaliados no próximo início do coletor.
- A entrada é montada pela persistência: janela de 120 s por papel de sensor em todos os segmentos da sessão e eventos relacionados recortados entre os incidentes vizinhos.

**Alternativas.** Modelo estatístico/ML (sem dados rotulados e inexplicável); árvore de decisão única (mistura regras, difícil de estender); texto pronto no banco (amarra idioma e impede reavaliação).

**Consequências.** A pontuação é heurística e os pesos são escolhas documentadas, não probabilidades. Mudar uma regra sem subir a versão deixaria diagnósticos antigos desatualizados: a regra está escrita na constante e no AGENTS do Domain.
