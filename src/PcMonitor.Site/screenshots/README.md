# Capturas de tela da landing page

As imagens em `public/screens/` são capturas do painel de verdade, convertidas para WebP. Os PNG originais ficam em `screenshots/raw/`, que não é versionada.

| Imagem | De onde vem | Como foi capturada |
|---|---|---|
| `overview` | Dados reais da máquina do dono (`http://127.0.0.1:5280/`) | Viewport 1600 × 1000, tema escuro |
| `history` | Dados reais (`/history`, +12 V, 24 horas) | Viewport 1600 × 1000 |
| `mini` | Dados reais (`/mini`) | Viewport 804 × 120 com `zoom: 3` no `<html>` (o painel tem 268 × 40 px) |
| `incident` | **Banco de demonstração** (`DemoDatabase.cs`) | Viewport 1600 × 1000, `/incidents/1` |

A captura do incidente usa dados sintéticos porque nenhum incidente real tinha telemetria quando a página foi feita. Por isso a legenda dela diz "dados de exemplo". O banco de demonstração é criado pelos stores e pelo motor de diagnóstico de verdade, e não contém nome de máquina nem de usuário.

## Refazer

1. Gerar o instalador (`powershell -File scripts/Build-Release.ps1`), que deixa o painel publicado em `artifacts/release/staging/Dashboard`.
2. Criar o banco de demonstração:

   ```powershell
   dotnet run src/PcMonitor.Site/screenshots/DemoDatabase.cs -- .superpowers/demo/demo.db
   ```

3. Subir um segundo painel sobre ele, na porta 5281 (o painel instalado continua na 5280):

   ```powershell
   artifacts/release/staging/Dashboard/PcMonitor.Dashboard.exe --Dashboard:DatabasePath=<caminho absoluto do demo.db> --Dashboard:Port=5281
   ```

4. Capturar as telas com o Playwright (ou outro navegador automatizado) nos tamanhos da tabela, esperando uns 4 s para os gráficos carregarem, e salvar em `screenshots/raw/<id>.png`. O Edge em modo headless (`--screenshot`) não serve: a conexão ao vivo (SSE) não deixa a página terminar de carregar.
5. **Conferir cada imagem**: nenhum nome de máquina, nenhum usuário, nenhum caminho pessoal.
6. Em `src/PcMonitor.Site`, rodar `pnpm screens` (precisa do `ffmpeg` no PATH: `winget install Gyan.FFmpeg`) e versionar `public/screens/`.
