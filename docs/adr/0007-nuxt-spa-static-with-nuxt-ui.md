# 0007 — Nuxt SPA estática com Nuxt UI

**Contexto.** O dono pediu Vue com Nuxt UI e uma interface bonita. A máquina do usuário não deve precisar de Node instalado.

**Decisão.** Nuxt 4 com `ssr: false`, gerado com `nuxt generate` e servido pelo ASP.NET Core a partir de `wwwroot`. Nuxt UI v4, tema escuro por padrão, cores semânticas em `app.config.ts`, i18n `pt-BR`/`en`. pnpm com política de idade mínima de pacotes e scripts de instalação liberados só por lista.

**Alternativas.** Nuxt com SSR em Node (terceiro processo e runtime Node na máquina); Vue + Vite sem Nuxt (perde roteamento por arquivo e o módulo oficial do Nuxt UI).

**Consequências.** Node/pnpm são necessários só para desenvolver e publicar. O visual final é decidido com mockups no Plano 05; a biblioteca de gráficos (Unovis ou a integração de gráficos do próprio Nuxt UI) é escolhida lá.
