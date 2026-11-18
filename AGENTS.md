# AuraScent (PROJETO-AURA-PARFUM)

Este arquivo **prevalece** sobre as skills globais do plugin `lucas-padroes` (stack-core, entrega-sdd-qualidade, appsec-lgpd-owasp, frontend-ui).

## Stack (não propor troca sem pedido explícito)
- .NET 10 + **.NET MAUI** (C#/XAML), MVVM com CommunityToolkit.Mvvm, alvos Android / iOS / MacCatalyst / Windows.
- App cliente, sem backend próprio. Consome APIs públicas: Scento (perfumes), Open-Meteo (clima/geocoding), 10ML (dados enriquecidos), MyMemory (tradução), FlagCDN (bandeiras).
- Estrutura: `AuraScent/{Views,ViewModels,Models,Services,Controls,Converters,Atmospheres}`; testes em `AuraScent.Tests`.

## O que vale das skills aqui
- **Vale:** regras de resposta (pt-BR, entregável primeiro, honestidade sobre gates), spec curta antes de feature (`docs/specs/NNN-nome.md`), testes para critério de aceite e regressão de bug, segurança e LGPD aplicáveis a app cliente, piso de qualidade de UI (acessibilidade, estados vazio/carregando/erro, alvos de toque ≥ 44px, microcopy).
- **Não vale:** tudo que é específico de Next.js/React/Prisma/Neon/Vercel/Render/Zod/Tailwind (Server Components, pooling, headers HTTP de servidor, cookies, deploy Vercel etc.).

## Quality gate equivalente
1. Build: `dotnet build AuraScent/AuraScent.csproj -f net10.0-windows10.0.19041.0` (e `-f net10.0-android` quando mexer em algo de plataforma) — 0 erros, sem warning novo.
2. Testes: `dotnet test AuraScent.Tests`.
3. Dependências: `dotnet list package --vulnerable --include-transitive` — sem alta/crítica.
4. Segredos: nenhuma chave de API no código ou no binário.
- Sem rodar, dizer "não executei os gates" e listar os comandos acima.

## Segurança e LGPD no app
- Cidade/localização do usuário é dado pessoal: usar só para a recomendação por clima, não registrar em log, não enviar a terceiros além da API de clima.
- Favoritos e preferências ficam só no dispositivo; token/segredo nunca em `Preferences` (usar `SecureStorage` se um dia existir).
- Respostas de APIs externas são entrada não confiável: parse tolerante, `try/catch` sem derrubar a tela, timeouts nos `HttpClient`, URLs montadas com `Uri.EscapeDataString`.

## UI
- Seguir o design system existente em `Resources/Styles` (`Colors.xaml`, `Styles.xaml`: `Glass`, `Chip`, `Eyebrow`, `Headline`, `Title`, `Body`, `Caption`, `PillButton`, `PrimaryButton`). Não criar cor hex solta nem um segundo estilo de botão/card.
- Evitar `CollectionView`/`ScrollView` com rolagem vertical aninhados dentro de outra rolagem vertical (trava o gesto).

