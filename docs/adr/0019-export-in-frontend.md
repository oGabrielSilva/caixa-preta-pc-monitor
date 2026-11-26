# 0019 — Exportação gerada no frontend

**Contexto.** O dono quis exportar as quedas e o histórico para CSV, Excel e PDF.

**Decisão.**
- Os arquivos são montados no navegador a partir dos mesmos dados e textos que a tela mostra (`app/utils/export/`): uma tabela neutra (`ExportTable`) vira CSV, planilha ou PDF. Os textos do diagnóstico vêm das mesmas chaves de i18n, então o arquivo sai no idioma da tela.
- CSV em UTF-8 com BOM, CRLF e `;` como separador quando o decimal do idioma é vírgula (o Excel em pt-BR abre direto). Valor ausente vira "—".
- Excel com `exceljs` (build de navegador) e PDF com `pdfmake` 0.3 (fontes no VFS, gráfico da telemetria em SVG). As duas bibliotecas são carregadas só ao exportar.
- Nomes: `caixa-preta-<tipo>-<AAAA-MM-DD-HHmm>.<ext>`. Lista de quedas: CSV, Excel, PDF; uma queda: PDF; histórico: CSV e Excel.

**Alternativas.** Endpoints de exportação no dashboard (duplicariam em C# os textos e as traduções do frontend e exigiriam bibliotecas de PDF e planilha no serviço).

**Consequências.** O serviço continua só de leitura e sem dependências novas. O bundle cresce apenas no chunk carregado ao exportar.
