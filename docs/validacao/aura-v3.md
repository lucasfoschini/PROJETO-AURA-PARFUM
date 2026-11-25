# Validação do redesign AURA V3

O app Windows foi compilado e executado em uma sessão interativa em 25/11/2026. As capturas são do executável original e do redesign, com consultas reais ao Open-Meteo e à Scento. A janela estreita do Windows permitiu verificar o layout compacto. A validação em Android, iOS e Mac Catalyst permanece pendente.

## Comparação visual

| Tela | Antes | Depois |
|---|---|---|
| Hoje | [Antes](aura-v3/antes-hoje.png) | [Depois](aura-v3/depois-hoje.png) |
| Explorar | [Antes](aura-v3/antes-explorar.png) | [Depois](aura-v3/depois-explorar.png) |
| Detalhes | [Antes](aura-v3/antes-detalhes.png) | [Depois](aura-v3/depois-detalhes.png) |
| Favoritos | [Antes](aura-v3/antes-favoritos.png) | [Depois](aura-v3/depois-favoritos.png) |
| Menu lateral | [Antes](aura-v3/antes-menu.png) | [Depois](aura-v3/depois-menu.png) |
| Sobre | | [Depois](aura-v3/depois-sobre.png) |

No Windows, o Shell mantém as abas na barra superior e abre o menu lateral sob demanda. A experiência olfativa está integrada ao detalhe. Os frascos e as paisagens são ilustrações nativas, e os campos de catálogo representam dados das APIs.

## Clima observado no aplicativo

As cidades abaixo foram consultadas por volta das 03h no horário de São Paulo. Temperatura e condição são os valores exibidos pelo aplicativo.

| Atmosfera | Consulta real | Condição | Captura em janela Windows de 430 px |
|---|---|---|---|
| Noturna | São Paulo, Brasil | 22° e nublado | [Noturna](aura-v3/tema-noturna.png) |
| Solar | Daca, Bangladesh | 26° e céu limpo | [Solar](aura-v3/tema-solar.png) |
| Chuvosa | Banguecoque, Tailândia | 34° e garoa | [Chuvosa](aura-v3/tema-chuvosa.png) |
| Fria | Tóquio, Japão | 13° e céu limpo | [Fria](aura-v3/tema-fria.png) |
| Amena | Taipé, Taiwan | 19° e nublado | [Amena](aura-v3/tema-amena.png) |

O layout de Hoje também foi observado a [320 px](aura-v3/hoje-320.png) e a [390 px](aura-v3/hoje-390.png). A largura de 320 px usa o menu de excedentes da barra nativa para Favoritos, que também tem atalho direto em Hoje.

## Critérios de aceitação

| Critério | Resultado | Evidência |
|---|---|---|
| Cinco páginas reais, menu, abas e pilha | PASSOU no Windows | Menu abriu e fechou. Sobre e Alterar cidade abriram seus destinos. As três abas ficaram acessíveis. O detalhe foi aberto por um card e Voltar retornou à busca com os resultados. |
| Botão físico Voltar e navegação em dispositivo | NÃO EXECUTADO | Emulador ou dispositivo Android indisponível para a validação. |
| Cidade com acentos, homônimos e bandeiras | PASSOU no Windows | Busca por São retornou São Paulo. Busca por Santiago mostrou opções com regiões e países, incluindo Chile, Cuba e Filipinas. Foram selecionadas sugestões com o país apresentado no hero. |
| Clima, recomendações e consistência | PASSOU | As cinco atmosferas apareceram em consultas reais. O teste determinístico verificou precedência e os limites de temperatura. Uma resposta antiga foi entregue depois da nova em teste, com validação da cidade, bandeira, temperatura, atmosfera e catálogo exibidos. |
| Busca, sugestões, resultados e estados | PASSOU para os fluxos exercitados | Busca rápida por Oud e detalhe real no Windows. O teste determinístico distinguiu início, resultado vazio e resultado encontrado. A seleção de cidade durante a busca manteve a identificação do resultado exibido. |
| Notas, tradução e enriquecimento | PASSOU no Windows | Oud Wood exibiu descrição traduzida, três níveis de notas, acordes, longevidade, projeção e a ação 10ML. Os campos dependem da resposta de cada fonte. |
| Favoritos e continuidade | PASSOU no Windows | Um favorito real apareceu na coleção, permaneceu como uma única entrada após reiniciar e desapareceu após a remoção. O estado anterior da coleção foi restaurado. |
| Fallbacks sob falha | PASSOU para falha determinística de clima | Na atualização com erro, o caso de teste manteve clima e recomendações da última consulta bem-sucedida. Falhas reais de catálogo, bandeiras e 10ML, links externos e coleção vazia ficaram sem teste interativo. |
| Layout e acessibilidade | PASSOU para a amostra Windows | Janela de 1220 px e Hoje a 320, 390 e 430 px. A árvore de acessibilidade expôs os botões de menu, busca, detalhe e favoritos. Leitor de tela falado, escala ampliada, contraste instrumental, teclado virtual e preferência de movimento reduzido ficaram sem teste. |
| Navegação repetida e renderização | PASSOU para o teste de alternância | 20 ciclos Hoje/Favoritos sem interrupções. Memória privada observada de 223,3 MiB para 230,5 MiB. FPS e retenção em sessões maiores permanecem pendentes de medição. |
| Stack técnica e provedores | PASSOU por revisão | C# e XAML, mesmos TFMs, configuração de SourceGen, application ID, referências NuGet, provedores e armazenamento. |

## Gates e estado do repositório

- `dotnet --info` e `dotnet workload list` confirmaram SDK 10.0.404 e MAUI em uma instalação temporária. O `dotnet` do PATH originalmente encontrava apenas runtimes.
- `dotnet build AuraScent/AuraScent.csproj -f net10.0-windows10.0.19041.0` passou com zero erros e 28 avisos preexistentes do MVVM Toolkit. No código anterior, o SourceGen gerava um erro de conversão numérica na grade percentual. Em uma cópia temporária, o uso do mesmo valor com o tipo correto permitiu medir a linha de base, com 40 avisos e zero erros. As capturas anteriores vieram do executável preexistente.
- `dotnet build AuraScent/AuraScent.csproj -f net10.0-android` encontrou `XA5300`, por falta do SDK Android. A tentativa de preparar o ambiente temporário chegou à etapa que exige aceite das licenças do SDK Android. A compilação para iOS e Mac Catalyst permanece pendente em um ambiente Apple.
- `dotnet test AuraScent.Tests` retornou `MSB1003`. A pasta de testes anterior contém código, mas carece de projeto de teste. As [verificações de regressão](../../AuraScent.Tests/AtmosphericRedesignChecks.cs) foram compiladas via Roslyn contra o binário Windows e executadas com `dotnet exec`, sem adicionar pacote.
- `dotnet list AuraScent/AuraScent.csproj package --vulnerable --include-transitive --no-restore` passou. A consulta à fonte oficial do NuGet não reportou pacotes vulneráveis. A pesquisa de formatos conhecidos de segredos no fonte e no binário do app não encontrou correspondências.
- `git diff --check` passou. O patch contém 28 arquivos rastreados alterados, com 1.454 inserções e 1.252 remoções no `git diff --stat`, além de novos controles, ícones, verificações e documentação. As alterações estão no workspace da branch `main`, que já estava um commit atrás de `origin/main`.
