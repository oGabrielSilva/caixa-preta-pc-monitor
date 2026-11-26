# 0018 — App da bandeja com WebView2 e painel fixo no lugar de notificações

**Contexto.** O painel abria no navegador e sumia atrás das outras janelas. O dono queria o app na bandeja do sistema e, em vez de notificações, um painelzinho sempre visível (como o do Teams numa reunião), com poucos números que ficam vermelhos quando algo sai do normal.

**Decisão.**
- Terceiro processo, `CaixaPreta.exe` (`src/PcMonitor.Tray`): WPF + `NotifyIcon` do Windows Forms, na sessão do usuário, sem privilégio. Hospeda o próprio painel em duas janelas WebView2 que compartilham um ambiente (`%LocalAppData%\CaixaPreta\WebView2`): a janela principal (o painel inteiro) e o painel fixo (rota `/mini`, 300×56, sempre por cima, arrastável, posição lembrada).
- O app só fala HTTP com o dashboard (`127.0.0.1`, porta lida do `appsettings.json` instalado). Toda regra (faixas, cores, estado da gravação) continua no dashboard; o app não referencia nenhum projeto PC Monitor.
- As páginas pedem ações ao app por `chrome.webview.postMessage`: `{type:"open", path}` (abre a janela principal naquela página) e `{type:"drag"}` (move o painel fixo).
- Uma instância por sessão (mutex + pipe nomeado: abrir de novo mostra a janela principal). "Iniciar com o Windows" é um valor em `HKCU\…\Run`; o app o liga uma vez, na primeira execução, e o usuário desliga pelo menu.
- Sem notificações do Windows: o painel fixo mostra temperatura e carga da CPU e as tensões +12 V, +5 V e +3,3 V, com o número em vermelho fora da faixa, e um ponto que indica gravação e queda não vista.

**Alternativas.** .NET MAUI HybridWebView (pesado para duas janelas pequenas e sem ganho no Windows); WinForms puro com gráficos nativos (duplicaria as telas e as regras); notificações toast (o dono preferiu ver os números o tempo todo).

**Consequências.** O WebView2 não fica transparente no WPF, então o painel fixo é um retângulo opaco. As telas existem uma vez só (Nuxt). A janela e o WebView2 são verificados rodando o app; a lógica (posição, configurações, instância única, endereço, inicialização) tem testes.
