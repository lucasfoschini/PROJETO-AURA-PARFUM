# AuraScent

Aplicativo multiplataforma de descoberta de perfumes feito com .NET MAUI. O AuraScent combina o clima atual da cidade com recomendações olfativas e oferece exploração, favoritos e uma experiência imersiva.

## Funcionalidades

- Consulta do clima e geocodificação por cidade usando a API Open-Meteo.
- Busca e detalhes de perfumes usando a API Scento Perfume.
- Recomendações relacionadas à atmosfera do clima.
- Navegação por abas e menu lateral, com telas de detalhes e experiência imersiva.
- Interface com temas visuais dinâmicos e animações.

## Tecnologias

- .NET 10 e .NET MAUI
- CommunityToolkit.Mvvm
- Injeção de dependência e `HttpClientFactory`
- APIs Open-Meteo e Scento Perfume

## Requisitos

- .NET 10 SDK.
- Visual Studio 2026 com a carga de trabalho **Desenvolvimento de interface do usuário de aplicativos multiplataforma .NET (MAUI)**, ou os workloads MAUI necessários para a plataforma-alvo.
- Para Android: workload Android e emulador ou dispositivo configurado.
- Para iOS e Mac Catalyst: Mac com Xcode. O desenvolvimento e a compilação dessas plataformas dependem do ambiente Apple.

## Executar

Abra `AuraScent.slnx` no Visual Studio, escolha um destino instalado (por exemplo, Android Emulator ou Windows Machine) e execute com F5.

Para restaurar e compilar Android pela linha de comando, a partir da raiz do repositório:

```powershell
dotnet restore .\AuraScent\AuraScent.csproj
dotnet build .\AuraScent\AuraScent.csproj -f net10.0-android
```

A compilação para Windows requer Windows e os componentes correspondentes instalados. iOS e Mac Catalyst requerem macOS/Xcode.

## Estrutura

- `AuraScent/Views` — páginas da aplicação.
- `AuraScent/ViewModels` — estado e comandos das telas.
- `AuraScent/Services` — integração com APIs e lógica de recomendação.
- `AuraScent/Atmospheres` — modelos de atmosfera relacionados ao clima.
- `AuraScent/Resources` — ícones, fontes, imagens e outros recursos MAUI.
- `AuraScent/Platforms` — configuração específica de cada plataforma.

## APIs

- [Open-Meteo](https://open-meteo.com/) — previsão atual e geocodificação, sem chave de API configurada no projeto.
- [Scento Perfume](https://www.scento.com/api/perfume/v1/) — catálogo e detalhes de perfumes.

As APIs são serviços externos; disponibilidade, formato das respostas e termos de uso dependem de seus respectivos provedores.
