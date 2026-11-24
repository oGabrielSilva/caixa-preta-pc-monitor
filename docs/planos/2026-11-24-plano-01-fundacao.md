# Plano 01 — Spike de sensores e fundação do projeto

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** validar que os sensores da máquina são legíveis e entregar o esqueleto completo do repositório (camadas, build, frontend, contrato de API, docs, CI) antes de qualquer funcionalidade.

**Architecture:** Ports & Adapters em .NET 10, com 7 projetos de código e testes de arquitetura. Dois hosts (Collector, Dashboard). O Dashboard serve uma SPA estática Nuxt 4 + Nuxt UI e expõe uma API cujo contrato OpenAPI gera os tipos TypeScript.

**Tech Stack:** .NET 10 (SDK 10.0.404) · LibreHardwareMonitorLib 0.9.6 · ASP.NET Core minimal API + Microsoft.AspNetCore.OpenApi · xUnit v3, Shouldly, NSubstitute, ArchUnitNET · Nuxt 4, Nuxt UI v4, @nuxtjs/i18n, openapi-typescript, openapi-fetch, Vitest, @nuxt/eslint · pnpm, Node 24 LTS · GitHub Actions · jscpd.

**Spec:** [docs/estudos/2026-11-24-estudo-arquitetura.md](../estudos/2026-11-24-estudo-arquitetura.md)

**Fora deste plano** (planos seguintes): coletor, SQLite e `scripts/install.ps1` (fase 2), incidentes (3), diagnóstico (4), painel com mockups, Unovis e o teste ponta a ponta com Playwright (5). Nenhum projeto ganha comportamento de negócio aqui.

## Global Constraints

- Target `net10.0` em todos os projetos; `global.json` com `"version": "10.0.404"`, `"rollForward": "latestFeature"`.
- `Directory.Build.props`: `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `LangVersion=latest`.
- Central Package Management: **nenhuma** `PackageReference` com `Version` fora de `Directory.Packages.props`. `LibreHardwareMonitorLib` fixado em **0.9.6**; demais pacotes na última versão estável no momento da execução.
- Solução no formato `PcMonitor.slnx`.
- Código, identificadores, commits e mensagens de build/log em **inglês**; documentação em **português**; README bilíngue (pt-BR primeiro, depois en).
- Licença **MIT**.
- Dashboard escuta **somente em `127.0.0.1`**, porta padrão **5280**.
- Frontend: Nuxt 4 com `ssr: false`, gerado com `nuxt generate`; Nuxt UI v4; TypeScript `strict`; idiomas `pt-BR` (padrão) e `en`; **pnpm** e **Node 24 LTS** (`.nvmrc` = `24`).
- DTOs só em C#. TypeScript da API é sempre gerado, nunca escrito à mão.
- Cada pasta com `AGENTS.md` tem um `CLAUDE.md` cujo conteúdo é exatamente `@AGENTS.md`.
- Commits são **locais** (branch `main`); nada é enviado ao GitHub neste plano.

## Review Focus

1. **Rota de API inexistente cair na SPA:** `GET /api/qualquer-coisa` precisa devolver 404 da API, nunca o `index.html`. Teste na Task 6.
2. **`ASPNETCORE_URLS=http://0.0.0.0:...` expor o painel na rede:** o painel continua só em `127.0.0.1`. Verificação na Task 3.
3. **Chave de tradução faltando num idioma:** `pt-BR.json` e `en.json` com o mesmo conjunto de chaves. Teste na Task 4.
4. **Contribuidor sem Node/pnpm:** `dotnet build` e `dotnet test` funcionam sem Node; só o `dotnet publish` do Dashboard exige pnpm e falha com mensagem clara. Verificação na Task 6.
5. **Tipos TypeScript desatualizados em relação à API:** o CI falha se o documento OpenAPI ou `schema.d.ts` regenerados diferirem do commit. Verificação nas Tasks 5 e 8.

---

### Task 0: Spike de sensores (descartável)

Responde as 4 perguntas da §10 do estudo. **Não entra na solução e não segue os padrões de qualidade.**

**Files:**
- Create: `spikes/Directory.Build.props` e `spikes/Directory.Packages.props`, ambos vazios (`<Project />`, com `ManagePackageVersionsCentrally=false` no segundo), para o spike **não herdar** as regras de build da raiz
- Create: `spikes/SensorProbe/SensorProbe.csproj` (console, `net10.0`, `PackageReference LibreHardwareMonitorLib` `Version="0.9.6"`)
- Create: `spikes/SensorProbe/app.manifest` (`requestedExecutionLevel level="requireAdministrator"`)
- Create: `spikes/SensorProbe/Program.cs`
- Create: `docs/estudos/2026-11-24-resultado-spike-sensores.md` (saída)

- [ ] **Step 1: Escrever o probe**

`Computer` com `IsCpuEnabled`, `IsMotherboardEnabled`, `IsMemoryEnabled` = true e todo o resto false. `Open()`, depois 5 ciclos de `Accept(UpdateVisitor)` medindo cada um com `Stopwatch`. Percorrer hardware → subhardware → sensores e imprimir uma tabela Markdown: `Hardware | Identifier | Name | SensorType | Value (último ciclo) | Min | Max`. No fim, imprimir o tempo de cada ciclo e uma lista de sensores com valor `null`.

- [ ] **Step 2: Rodar como administrador**

O usuário abre um terminal **como administrador** (Claude não aprova UAC) e roda:
`dotnet run --project spikes/SensorProbe -c Release > docs/estudos/2026-11-24-resultado-spike-sensores.md`

Se a lib pedir a instalação do driver **PawnIO**: parar e pedir ao usuário que o instale (download e execução de driver precisam da autorização explícita dele). Registrar no resultado como foi feito.

- [ ] **Step 3: Interpretar e registrar a decisão**

Acrescentar ao fim do arquivo de resultado uma seção `## Decisão` com:
- chip Super I/O identificado;
- se existem rails +12 V, +5 V e +3,3 V e seus valores (dentro de 11,40–12,60 / 4,75–5,25 / 3,135–3,465?);
- se existem Tctl/Tdie, Vcore, SoC e a potência do pacote;
- tempo máximo de ciclo e sensores problemáticos;
- o resultado da tabela de decisão da §10: **segue**, **segue sem regras de rail** ou **para**.

Se o resultado for **para**: interromper o plano e voltar a falar com o usuário.

- [ ] **Step 4: Commit** (após o `git init` da Task 1; se a Task 1 ainda não rodou, commitar junto com ela)

```bash
git add spikes/ docs/estudos/2026-11-24-resultado-spike-sensores.md
git commit -m "spike: probe hardware sensors with LibreHardwareMonitor"
```

---

### Task 1: Repositório e propriedades de build

**Files:**
- Create: `.gitignore` (template `dotnet new gitignore` + `node_modules/`, `.nuxt/`, `.output/`, `artifacts/`)
- Create: `.gitattributes` (`* text=auto eol=lf`; `*.ps1 text eol=crlf`)
- Create: `.editorconfig` (convenções .NET: file-scoped namespaces, `var` quando o tipo é aparente, `_camelCase` em campos privados, `Async` em métodos assíncronos, diagnósticos de estilo como `warning`; e regras para TS/Vue com indentação de 2)
- Create: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `PcMonitor.slnx`
- Create: `LICENSE` (MIT, titular "PC Monitor contributors", ano 2026)
- Create: `README.md` (pt-BR e en: o que é, status "em construção", link para o estudo)
- Create: `.nvmrc` (`24`)

- [ ] **Step 1: `git init -b main`** e criar os arquivos acima.
- [ ] **Step 2: Verificar** — `dotnet --version` mostra `10.0.404` na raiz; `dotnet build PcMonitor.slnx` termina com "Build succeeded" (solução ainda vazia).
- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "chore: initialize repository and shared build settings"
```

---

### Task 2: Projetos das camadas e testes de arquitetura

**Files:**
- Create (classlib): `src/PcMonitor.Domain`, `src/PcMonitor.Application`, `src/PcMonitor.Infrastructure.Sensors.Lhm`, `src/PcMonitor.Infrastructure.Persistence.Sqlite`, `src/PcMonitor.Infrastructure.Windows`
- Create (worker): `src/PcMonitor.Collector` (`Microsoft.NET.Sdk.Worker`)
- Create (web): `src/PcMonitor.Dashboard` (`Microsoft.NET.Sdk.Web`)
- Create (xunit v3): `tests/PcMonitor.Domain.Tests`, `tests/PcMonitor.Application.Tests`, `tests/PcMonitor.Infrastructure.Persistence.Sqlite.Tests`, `tests/PcMonitor.Architecture.Tests`
- Create: `AGENTS.md` + `CLAUDE.md` (`@AGENTS.md`) na raiz, em cada `src/*` e em cada `tests/*`
- Test: `tests/PcMonitor.Architecture.Tests/LayerDependencyTests.cs`

**Referências entre projetos (exatas):**

| Projeto | Referencia |
|---|---|
| Domain | — (nenhum pacote) |
| Application | Domain |
| Infrastructure.* | Application |
| Collector | Application, Infrastructure.Sensors.Lhm, Infrastructure.Persistence.Sqlite, Infrastructure.Windows |
| Dashboard | Application, Infrastructure.Persistence.Sqlite |
| *.Tests | o projeto testado; Architecture.Tests referencia todos os de `src/` |

Pacotes neste momento: `LibreHardwareMonitorLib` em Sensors.Lhm; `Microsoft.Data.Sqlite` e `Dapper` em Persistence.Sqlite; `Microsoft.Extensions.Hosting.WindowsServices` e `Microsoft.Extensions.Logging.EventLog` no Collector. Testes: `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Shouldly`; `TngTech.ArchUnitNET.xUnitV3` em Architecture.Tests.

**Conteúdo de cada `AGENTS.md` de projeto** (curto): responsabilidade em 1–2 frases; pode depender de; **não** pode depender de; como testar (`dotnet test tests/...`). O `AGENTS.md` raiz tem: visão do produto em 3 linhas, mapa de pastas, comandos (build, test, format, frontend), regras globais (copiadas das Global Constraints deste plano), link para o estudo e para `docs/architecture/`.

- [ ] **Step 1: Criar os projetos** com as referências e pacotes da tabela, cada um com seu `AGENTS.md`/`CLAUDE.md`, e adicioná-los ao `PcMonitor.slnx` em pastas de solução `src` e `tests`. As bibliotecas ficam **sem tipos** por enquanto: as regras abaixo passam de forma vazia e começam a valer quando os tipos chegarem nos planos seguintes. Verificar: `dotnet build PcMonitor.slnx` sem avisos.

- [ ] **Step 2: Escrever os testes de arquitetura**

```csharp
// tests/PcMonitor.Architecture.Tests/LayerDependencyTests.cs
public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_references_only_the_base_class_library()
    {
        var referenced = Assembly.Load("PcMonitor.Domain").GetReferencedAssemblies()
            .Select(a => a.Name!);
        referenced.ShouldAllBe(n => n.StartsWith("System") || n == "netstandard");
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_hosts() { /* ArchUnitNET:
        Types().That().ResideInNamespace("PcMonitor.Application", true)
          .Should().NotDependOnAny(Types().That().ResideInNamespace("PcMonitor.Infrastructure", true)
              .Or().ResideInNamespace("PcMonitor.Collector", true)
              .Or().ResideInNamespace("PcMonitor.Dashboard", true)) */ }

    [Theory]
    [InlineData("PcMonitor.Infrastructure.Sensors.Lhm")]
    [InlineData("PcMonitor.Infrastructure.Persistence.Sqlite")]
    [InlineData("PcMonitor.Infrastructure.Windows")]
    public void Infrastructure_adapters_do_not_depend_on_each_other(string adapter) { /* ArchUnitNET:
        types in `adapter` NotDependOnAny types in the other two Infrastructure namespaces */ }

    [Fact]
    public void Dashboard_does_not_depend_on_sensor_or_windows_adapters() { /* ArchUnitNET */ }

    [Fact]
    public void Only_the_sensor_adapter_uses_LibreHardwareMonitor() { /* ArchUnitNET:
        types outside PcMonitor.Infrastructure.Sensors.Lhm NotDependOnAny type in "LibreHardwareMonitor" */ }
}
```

A arquitetura ArchUnitNET é carregada uma vez numa classe `ArchitectureFixture` (`ArchLoader().LoadAssemblies(...)` com os 7 assemblies de `src/`).

- [ ] **Step 3: Rodar e ver passar** — `dotnet test PcMonitor.slnx` → todos verdes.
- [ ] **Step 4: Provar que as regras mordem** (substitui o "ver falhar", já que a estrutura é anterior ao teste):
  - temporariamente adicionar `ProjectReference` de Sensors.Lhm para Persistence.Sqlite, uma classe `TempProbe` em Persistence.Sqlite e uma classe em Sensors.Lhm com um campo `TempProbe` → `Infrastructure_adapters_do_not_depend_on_each_other` falha;
  - temporariamente adicionar `PackageReference Microsoft.Extensions.Logging.Abstractions` ao Domain e uma classe que use `ILogger` → `Domain_references_only_the_base_class_library` falha.
  - Reverter tudo e confirmar verde.
- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add layered projects with architecture tests"
```

---

### Task 3: Hosts mínimos (Collector e Dashboard)

**Files:**
- Modify: `src/PcMonitor.Collector/Program.cs`
- Modify: `src/PcMonitor.Dashboard/Program.cs`
- Create: `src/PcMonitor.Dashboard/Configuration/DashboardOptions.cs`
- Create: `src/PcMonitor.Dashboard/Api/ApiEndpoints.cs` (`MapApi(this IEndpointRouteBuilder)`: cria o grupo `/api` e chama cada `Map*Endpoints`)
- Create: `src/PcMonitor.Dashboard/Api/Health/HealthEndpoints.cs`, `HealthResponse.cs`
- Create: `src/PcMonitor.Dashboard/appsettings.json` (`"Dashboard": { "Port": 5280 }`)
- Create: `tests/PcMonitor.Dashboard.Tests/` (xunit v3 + `Microsoft.AspNetCore.Mvc.Testing`), com `AGENTS.md`/`CLAUDE.md`
- Test: `tests/PcMonitor.Dashboard.Tests/Api/HealthEndpointsTests.cs`, `Configuration/DashboardOptionsTests.cs`

**Interfaces:**
- Produces: `public sealed record HealthResponse(string Status, string Version);` · `GET /api/health` → 200 `HealthResponse("ok", <versão do assembly>)` · `public sealed class DashboardOptions { public const string SectionName = "Dashboard"; [Range(1024, 65535)] public int Port { get; init; } = 5280; }` · padrão "um arquivo `*Endpoints.cs` por recurso, com uma extensão `Map{Recurso}Endpoints(this RouteGroupBuilder)`"

- [ ] **Step 1: Testes que falham**

```csharp
[Fact] public async Task Health_returns_ok_with_version()
{
    await using var factory = new WebApplicationFactory<Program>();
    var response = await factory.CreateClient().GetFromJsonAsync<HealthResponse>("/api/health");
    response!.Status.ShouldBe("ok");
    response.Version.ShouldNotBeNullOrWhiteSpace();
}

[Theory, InlineData(80), InlineData(70000)]
public void Port_outside_range_fails_validation(int port)
{
    var options = new DashboardOptions { Port = port };
    Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true)
        .ShouldBeFalse();
}

[Fact] public void Default_port_is_5280() => new DashboardOptions().Port.ShouldBe(5280);
```

- [ ] **Step 2: Rodar e ver falhar** — `dotnet test tests/PcMonitor.Dashboard.Tests` → erro de compilação (`HealthResponse` não existe).
- [ ] **Step 3: Implementar**
  - Dashboard: `AddOptions<DashboardOptions>().BindConfiguration(SectionName).ValidateDataAnnotations().ValidateOnStart()`; `builder.WebHost.ConfigureKestrel` com `Listen(IPAddress.Loopback, port)` (o endereço **não** é configurável); `AddOpenApi()`; `MapOpenApi()` em `/openapi/v1.json`; `app.MapApi()`. Se o compilador não expuser `Program` para os testes, adicionar `public partial class Program;`.
  - Collector: `Host.CreateApplicationBuilder(args)`, `AddWindowsService(o => o.ServiceName = "PcMonitor.Collector")`, logging `AddEventLog(s => s.SourceName = "PcMonitor.Collector")` só quando `OperatingSystem.IsWindows()`. Sem workers ainda.
- [ ] **Step 4: Rodar e ver passar** — `dotnet test PcMonitor.slnx` → verde.
- [ ] **Step 5: Verificar o bind** (Review Focus 2) — em PowerShell: `$env:ASPNETCORE_URLS='http://0.0.0.0:5000'; dotnet run --project src/PcMonitor.Dashboard`; noutro terminal `Get-NetTCPConnection -State Listen -LocalPort 5280,5000`. Esperado: só `127.0.0.1:5280`, nada em `5000`.
- [ ] **Step 6: Commit** — `git commit -m "feat: add minimal collector and dashboard hosts"`

---

### Task 4: Frontend Nuxt 4 + Nuxt UI

**Files** (todos em `src/PcMonitor.Dashboard.Web/`):
- Create: `package.json` (`packageManager: pnpm@<atual>`, `engines.node: ">=24"`; scripts `dev`, `generate`, `lint`, `typecheck`, `test`, `gen:api`), `pnpm-lock.yaml`
- Create: `nuxt.config.ts` (`ssr: false`; módulos `@nuxt/ui`, `@nuxtjs/i18n`, `@nuxt/eslint`, `@nuxt/test-utils/module`; `css: ['~/assets/css/main.css']`; `colorMode.preference: 'dark'`; `i18n: { defaultLocale: 'pt-BR', strategy: 'no_prefix', locales: [{ code: 'pt-BR', file: 'pt-BR.json' }, { code: 'en', file: 'en.json' }] }`; `vite.server.proxy: { '/api': 'http://127.0.0.1:5280' }`; `typescript.strict: true`)
- Create: `app/assets/css/main.css` (`@import "tailwindcss"; @import "@nuxt/ui";`)
- Create: `app/app.config.ts` (cores semânticas do Nuxt UI: `primary`, `success` = dentro da faixa, `warning` = atenção, `error` = fora da faixa, `neutral`)
- Create: `app/app.vue` (`<UApp><NuxtLayout><NuxtPage /></NuxtLayout></UApp>`)
- Create: `app/layouts/default.vue` (casca: barra lateral com a navegação e seletor de tema; **visual provisório**, refeito na fase 5 com mockups)
- Create: `app/utils/navigation.ts`, `app/pages/index.vue` (Ao vivo), `app/pages/incidents/index.vue`, `app/pages/history.vue` (cada página só com título traduzido e um `UEmpty`/placeholder)
- Create: `i18n/locales/pt-BR.json`, `i18n/locales/en.json`
- Create: `eslint.config.mjs` (via `withNuxt`), `vitest.config.ts` (`defineVitestConfig`, ambiente `nuxt`)
- Create: `AGENTS.md` + `CLAUDE.md` (estrutura `pages/components/composables/utils`, "componentes não chamam a API diretamente", comandos)
- Test: `test/unit/locales.spec.ts`, `test/unit/navigation.spec.ts`

**Interfaces:**
- Produces: `export interface NavigationItem { labelKey: string; icon: string; to: string }` · `export const navigationItems: readonly NavigationItem[]` com `to` = `/`, `/incidents`, `/history` e `labelKey` = `nav.live`, `nav.incidents`, `nav.history`.

- [ ] **Step 1: Criar o projeto** com `pnpm create nuxt@latest` (template mínimo) e `pnpm add @nuxt/ui tailwindcss @nuxtjs/i18n` + dev deps `@nuxt/eslint eslint @nuxt/test-utils vitest @vue/test-utils happy-dom vue-tsc typescript`. Ajustar os arquivos acima.
- [ ] **Step 2: Testes que falham**

```ts
// test/unit/locales.spec.ts
import ptBR from '~~/i18n/locales/pt-BR.json'
import en from '~~/i18n/locales/en.json'
const keys = (o: object, p = ''): string[] => Object.entries(o).flatMap(([k, v]) =>
  typeof v === 'object' ? keys(v, `${p}${k}.`) : [`${p}${k}`])
it('pt-BR and en have the same keys', () => expect(keys(en).sort()).toEqual(keys(ptBR).sort()))

// test/unit/navigation.spec.ts
it('every navigation label exists in pt-BR', () =>
  navigationItems.forEach(i => expect(keys(ptBR)).toContain(i.labelKey)))
it('navigation routes are unique', () =>
  expect(new Set(navigationItems.map(i => i.to)).size).toBe(navigationItems.length))
```

(`keys` fica em `test/support/locale-keys.ts`, importado pelos dois testes, sem duplicação.)

- [ ] **Step 3: Rodar e ver falhar** — `pnpm test` → falha (`navigationItems` não existe / chaves ausentes).
- [ ] **Step 4: Implementar** `navigation.ts`, os dois JSONs (`nav.live` "Ao vivo"/"Live", `nav.incidents` "Incidentes"/"Incidents", `nav.history` "Histórico"/"History", `app.title` "PC Monitor", `theme.toggle` …), o layout e as páginas.
- [ ] **Step 5: Rodar e ver passar** — `pnpm lint && pnpm typecheck && pnpm test` → tudo verde. `pnpm generate` gera `.output/public/index.html`.
- [ ] **Step 6: Commit** — `git commit -m "feat: scaffold Nuxt UI dashboard frontend"`

---

### Task 5: Contrato OpenAPI → TypeScript

**Files:**
- Modify: `Directory.Packages.props` + `src/PcMonitor.Dashboard/PcMonitor.Dashboard.csproj` (`Microsoft.Extensions.ApiDescription.Server`; `<OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>`; `<OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/openapi</OpenApiDocumentsDirectory>`)
- Create (gerado, commitado): `src/PcMonitor.Dashboard/openapi/PcMonitor.Dashboard.json`
- Modify: `src/PcMonitor.Dashboard.Web/package.json` (`gen:api`: `openapi-typescript ../PcMonitor.Dashboard/openapi/PcMonitor.Dashboard.json -o app/api/schema.d.ts`; deps `openapi-fetch`, dev `openapi-typescript`)
- Create (gerado, commitado): `src/PcMonitor.Dashboard.Web/app/api/schema.d.ts`
- Create: `app/api/client.ts`, `app/composables/useAppVersion.ts`
- Modify: `app/layouts/default.vue` (mostra a versão no rodapé)
- Modify: `eslint.config.mjs`, `.gitattributes` (ignorar/marcar `schema.d.ts` como `linguist-generated`)
- Test: `test/unit/useAppVersion.spec.ts`

**Interfaces:**
- Consumes: `GET /api/health` → `HealthResponse` (Task 3).
- Produces: `export const api = createClient<paths>({ baseUrl: '' })` em `app/api/client.ts` (único ponto que cria cliente HTTP) · `useAppVersion(): { version: Ref<string | null> }`.

- [ ] **Step 1: Gerar o documento** — `dotnet build src/PcMonitor.Dashboard` cria `openapi/PcMonitor.Dashboard.json` contendo `/api/health` e o schema `HealthResponse`.
- [ ] **Step 2: Gerar os tipos** — `pnpm gen:api` cria `schema.d.ts` com `paths['/api/health']`.
- [ ] **Step 3: Teste que falha**

```ts
it('exposes the version returned by /api/health', async () => {
  registerEndpoint('/api/health', () => ({ status: 'ok', version: '0.1.0' }))
  const { version } = useAppVersion()
  await flushPromises()
  expect(version.value).toBe('0.1.0')
})
```

- [ ] **Step 4: Rodar e ver falhar** → `useAppVersion` não existe.
- [ ] **Step 5: Implementar** `client.ts` e `useAppVersion` (usa `api.GET('/api/health')`; `null` enquanto carrega ou em erro).
- [ ] **Step 6: Rodar e ver passar** — `pnpm typecheck && pnpm test`.
- [ ] **Step 7: Verificar o Review Focus 5 localmente** — mudar `HealthResponse` (adicionar uma propriedade), `dotnet build` + `pnpm gen:api`, conferir que `git diff` mostra os dois arquivos gerados alterados. Reverter.
- [ ] **Step 8: Commit** — `git commit -m "feat: generate typed API client from OpenAPI"`

---

### Task 6: Publicação — SPA servida pelo Dashboard

**Files:**
- Create: `src/PcMonitor.Dashboard/build/DashboardWeb.targets` (importado pelo `.csproj`)
- Modify: `src/PcMonitor.Dashboard/Program.cs` (arquivos estáticos e fallback)
- Create: `src/PcMonitor.Dashboard/Api/ApiNotFound.cs` ou incluir em `ApiEndpoints.cs`: `group.MapFallback(() => Results.NotFound())` dentro do grupo `/api`
- Test: `tests/PcMonitor.Dashboard.Tests/SpaHostingTests.cs`

- [ ] **Step 1: Testes que falham**

```csharp
// fixture: diretório temporário com index.html "<!doctype html><title>spa</title>";
// client = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseWebRoot(tempDir)).CreateClient()
[Fact] public async Task Unknown_api_route_returns_404_not_the_spa()
{
    var response = await client.GetAsync("/api/does-not-exist");
    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    (await response.Content.ReadAsStringAsync()).ShouldNotContain("<title>spa</title>");
}

[Theory, InlineData("/"), InlineData("/incidents"), InlineData("/history")]
public async Task Client_routes_serve_the_spa(string path) =>
    (await client.GetStringAsync(path)).ShouldContain("<title>spa</title>");
```

- [ ] **Step 2: Rodar e ver falhar.**
- [ ] **Step 3: Implementar** `UseDefaultFiles()`, `UseStaticFiles()`, `MapFallbackToFile("index.html")` e o fallback 404 dentro de `/api`.
- [ ] **Step 4: Rodar e ver passar.**
- [ ] **Step 5: Alvo MSBuild** em `DashboardWeb.targets`:
  - `Target Name="BuildDashboardWeb" BeforeTargets="PrepareForPublish" Condition="'$(SkipDashboardWeb)' != 'true'"`
  - `Exec Command="pnpm --version"` com `ContinueOnError="true"` e `<Error Text="pnpm is required to publish PcMonitor.Dashboard. Install Node 24 LTS and run 'corepack enable'. Use -p:SkipDashboardWeb=true to publish without the UI." Condition="'$(MSBuildLastTaskResult)' == 'false'" />`
  - `pnpm install --frozen-lockfile` e `pnpm generate` com `WorkingDirectory` = `../PcMonitor.Dashboard.Web`
  - `ItemGroup` adicionando `.output/public/**` a `ResolvedFileToPublish` com `RelativePath="wwwroot/%(RecursiveDir)%(Filename)%(Extension)"`
  - **Nada disso roda em `build`**, só em `publish` (Review Focus 4).
- [ ] **Step 6: Verificar**
  - `dotnet build PcMonitor.slnx` e `dotnet test PcMonitor.slnx` com o pnpm fora do PATH → sucesso.
  - `dotnet publish src/PcMonitor.Dashboard -c Release -o artifacts/dashboard` → `artifacts/dashboard/wwwroot/index.html` existe.
  - Rodar `artifacts/dashboard/PcMonitor.Dashboard.exe`; `http://127.0.0.1:5280/` mostra o layout com a versão no rodapé; `/api/health` responde JSON.
  - Com o pnpm fora do PATH, `dotnet publish` falha com a mensagem acima.
- [ ] **Step 7: Commit** — `git commit -m "feat: serve generated SPA from dashboard on publish"`

---

### Task 7: Documentação de arquitetura, ADRs e verificação dos AGENTS.md

**Files:**
- Create: `docs/architecture/README.md` (índice), `overview.md` (contexto, contêineres, camadas: diagramas das §5.1–5.2 do estudo), `flows.md` (§6.1–6.3), `data-model.md` (ER da §7, marcado "planejado — fase 2"), `frontend.md` (estrutura do Nuxt, fluxo do contrato OpenAPI → TS em Mermaid)
- Create: `docs/adr/README.md` (formato: Contexto · Decisão · Alternativas · Consequências) e os ADRs:
  `0001-dotnet-10-lts.md` · `0002-librehardwaremonitor-pawnio.md` · `0003-collector-dashboard-split-least-privilege.md` · `0004-sqlite-wal-full-sync-frame-rows.md` · `0005-event-log-as-incident-source-of-truth.md` · `0006-no-native-aot.md` · `0007-nuxt-spa-static-with-nuxt-ui.md` · `0008-openapi-generated-typescript-contract.md` · `0009-agents-md-single-source.md`
- Create: `scripts/Test-AgentDocs.ps1`
- Modify: `AGENTS.md` raiz (links para `docs/architecture/` e `docs/adr/`)

Os ADRs resumem o estudo. **Não copiam** diagramas: apontam para `docs/architecture/`, onde cada diagrama existe uma vez só.

- [ ] **Step 1: Escrever `Test-AgentDocs.ps1`** — para cada `AGENTS.md` no repositório (exceto `node_modules`, `bin`, `obj`, `.nuxt`, `.output`): existe `CLAUDE.md` irmão com conteúdo (sem espaços nas pontas) igual a `@AGENTS.md`; e cada pasta direta de `src/` e `tests/` tem um `AGENTS.md`. Sai com código 1 e lista as violações.
- [ ] **Step 2: Provar que ele morde** — renomear temporariamente um `CLAUDE.md` → o script sai com 1 e cita o caminho. Restaurar → sai com 0.
- [ ] **Step 3: Escrever os documentos e ADRs.**
- [ ] **Step 4: Verificar** que todo bloco ```` ```mermaid ```` renderiza (preview do VS Code ou `npx -y @mermaid-js/mermaid-cli` em cada arquivo, numa pasta temporária fora do repo) e que todos os links relativos resolvem.
- [ ] **Step 5: Commit** — `git commit -m "docs: add architecture docs, ADRs and agent docs check"`

---

### Task 8: CI e verificação de duplicação

**Files:**
- Create: `.github/workflows/ci.yml`
- Create: `.jscpd.json`

**`.jscpd.json`:** `"threshold": 1`, `"minTokens": 50`, `"reporters": ["console"]`, `"path": ["src", "tests"]`, `"ignore"`: `**/bin/**`, `**/obj/**`, `**/node_modules/**`, `**/.nuxt/**`, `**/.output/**`, `**/openapi/*.json`, `**/app/api/schema.d.ts`, `**/pnpm-lock.yaml`, `**/*.Designer.cs`.

**`ci.yml`** — gatilhos `push` e `pull_request`; um job `build` em `windows-latest`:
1. `actions/checkout`, `actions/setup-dotnet` (`global-json-file: global.json`), `pnpm/action-setup`, `actions/setup-node` (`node-version-file: .nvmrc`, `cache: pnpm`, `cache-dependency-path: src/PcMonitor.Dashboard.Web/pnpm-lock.yaml`)
2. `dotnet restore` → `dotnet build -c Release --no-restore` → `dotnet format --verify-no-changes --no-restore` → `dotnet test -c Release --no-build`
3. Em `src/PcMonitor.Dashboard.Web`: `pnpm install --frozen-lockfile` → `pnpm lint` → `pnpm typecheck` → `pnpm test` → `pnpm gen:api`
4. `git diff --exit-code -- src/PcMonitor.Dashboard/openapi src/PcMonitor.Dashboard.Web/app/api` (Review Focus 5)
5. `pwsh scripts/Test-AgentDocs.ps1`
6. `pnpm dlx jscpd@<versão fixada>` na raiz

- [ ] **Step 1: Escrever os dois arquivos.**
- [ ] **Step 2: Rodar localmente cada comando dos passos 2–6** na ordem do workflow. Esperado: todos com código 0 e o jscpd reportando duplicação abaixo de 1%.
- [ ] **Step 3: Provar que o jscpd morde** — duplicar temporariamente um arquivo `.cs` de teste com outro nome → o jscpd falha. Remover.
- [ ] **Step 4: Validar a sintaxe do workflow** — `pnpm dlx @action-validator/cli .github/workflows/ci.yml` (ou equivalente) sem erros. O workflow só roda de verdade quando o repositório for enviado ao GitHub, fora deste plano.
- [ ] **Step 5: Commit** — `git commit -m "ci: add build, test, contract and duplication checks"`

---

## Critério de pronto do plano

- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `pnpm lint/typecheck/test`, `Test-AgentDocs.ps1` e jscpd verdes localmente.
- `dotnet publish` do Dashboard gera um executável que abre o painel provisório em `http://127.0.0.1:5280`, com a versão vinda da API.
- Resultado do spike registrado, com a decisão da §10.
- Nenhuma regra de negócio implementada; próximo passo: **Plano 02 — Coletor e persistência**.
