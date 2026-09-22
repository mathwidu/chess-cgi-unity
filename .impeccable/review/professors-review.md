disposition: fix

Substituição: subagente real de revisão usando `degraded/finish-reviewer.md`, pois o papel especializado não existe no harness. Escopo: menu atual; a captura do usuário foi tratada como rejeição do menu anterior. Entradas opcionais não examinadas: help/match/selection/promotion/error e `GameHud.Match.cs`. DESIGN.md será produzido depois; não há comp aprovado, imagem de decisão concluída ou detector aplicável a este Canvas Unity.

## persistence

Pass. `PRODUCT.md`, contrato, superfície, notas de direção e proveniência estão presentes. A resposta `29cc4bd4.answer.json` confirma `buildPath: code`, `buildPathFlipped: true` e a escolha de Palco da turma. O seed `9d5682be` aparece no contrato e nas notas. O padrão persistente `comp` não anula essa escolha da sessão; state/spec/diffs de comp não são exigidos aqui. A ausência atual de DESIGN.md segue a sequência informada de documentação posterior à revisão.

Pass na evidência: desktop 1920×1080, laptop 1280×800, compact 1024×768, wide 2560×1080, editor 1223×704, black-hard e local 1920×1080 foram abertos individualmente; dimensões verificadas com `sips`. Todos mostram conteúdo completo e compatível com o estado declarado, sem captura vazia ou corte de controles. São renders de integração descritos pelo produtor, não uma observação independente de Play no checkout desktop; a equivalência de código e o resultado 13/13 PlayMode foram informados pelo produtor e não reexecutados nesta revisão.

## fidelity

Sem comp aprovado: a tabela avalia pedido atual e contrato, não reprodução da imagem Highlife. A preferência opcional de acabamento continua sem resposta; a escolha do verde/luz/amarelo é uma proposta implementada, e este parecer não registra aprovação visual do usuário.

| Elemento/promessa | Estado | Evidência |
| --- | --- | --- |
| THESIS — elenco como apresentação do jogo | match | Dois professores reais ocupam a maior área visual; não há grade de cartões ou terceiro personagem. |
| TYPE — voz tipográfica própria e hierarquia | match | Lato Black no título e família Lato nos controles, confirmados no código e em ORIGINS.md; as notas a identificam como alternativa redistribuível, sem alegar que seja Avenir. Títulos, nomes e campos têm níveis distintos. A perda de legibilidade após redução está registrada separadamente. |
| MATERIAL — professores e peças reais | match | Modelos 3D texturizados, bases branca e preta e iluminação física; não há ilustração sintética disfarçada de asset existente ou imitação gráfica de material. |
| GROUND — campo verde profundo Feevale | match | O campo contínuo aparece verde escuro nas sete imagens, sem deriva azul/cinza. A autoridade é a cor nomeada em OWN-WORLD; não existe comp com valor exato para comparação. |
| Assinaturas institucionais | match | Xadrez CGI e Universidade Feevale separados, logo sem achatamento aparente, crédito acadêmico no rodapé. Proporção calculada a partir da textura no código; ORIGINS.md documenta o original 950×369 e NPOT None. |
| FIRST VIEWPORT — destaque conforme lado | match | desktop: Marta maior à frente, base branca, nome e papel corretos. black-hard: Ricardo maior, base preta, nome e papel corretos. local: ambos com peso visual próximo, legenda conjunta. |
| STORY — modo, lado, dificuldade, jogar | match | Ordem e agrupamento claros; local remove escolhas de IA e explica a alternância. A chamada amarela é a ação principal. |
| Responsividade e legibilidade operacional | contradicted | compact/editor apenas reduzem a mesma composição. Em 1024×768, os rótulos de 19 unidades e resumo de 18 caem visualmente para aproximadamente 11–12 px; ajuda e opções ficam pequenas. Há espaço vertical livre que poderia sustentar um arranjo mais legível. Não há overflow, mas caber não preservou a leitura confortável. `RefreshStartMenu` aplica escala uniforme ao bloco 1600×960. |
| Estado ativo versus foco | contradicted | Em local.png, Contra IA permanece com preenchimento verde mais luminoso que Dois jogadores, embora o sublinhado e o conteúdo indiquem modo local. `CreateButton` usa o mesmo preenchimento luminoso para hover/foco e `SetChoice` usa fundo mais discreto para o valor escolhido. A distinção existe pelo sublinhado, mas a saliência maior comunica o valor contrário. |
| FORM e movimento de seleção | match | Seed e direção corroborados no pacote; tropicalismo e conteúdo musical não foram importados. As imagens confirmam os extremos da seleção; o código confirma interpolação exponencial de escala/profundidade/material. A qualidade temporal da transição não foi auditada em vídeo. |

A primeira tela passa no teste de reconhecimento: os dois professores, a Feevale e a chamada amarela identificam este projeto. O problema de acabamento material remanescente está na escala de leitura e nos estados dos controles, não em falta de ornamento. Nas capturas do menu não há eyebrow, glyph usado como ícone, gradiente tipográfico, sombra dura decorativa, cartões aninhados ou fonte de sistema como voz principal. Não foram identificadas alegações comerciais ou conteúdo sintético apresentado como fato.

## ceiling

O card Highlife estabelece compromisso e acabamento; suas texturas, lettering artesanal, molduras e motivos musicais não são obrigações desta tradução institucional. A versão Unity usa sua matéria própria — modelos, luz e movimento de seleção — e o campo sóbrio é coerente com OWN-WORLD. Profundidade de um palco comum e sombras de contato poderiam ser exploradas futuramente, mas são possibilidades de direção, não achados materiais contra este contrato. O teto atual é limitado concretamente pela redução excessiva de texto e pela disputa visual entre foco e valor ativo.

## material_fixes

1. **Floor / cobertura e leitura:** ajustar o menu para compact/editor com reflow ou redistribuição dos espaços, mantendo texto operacional legível em pixels finais; usar como alvo rótulos/resumo de pelo menos cerca de 14 px e escolhas/ajuda de 16 px, sem ampliar apenas a captura. Revalidar 1024×768 e 1223×704 com os dois modelos, marca e CTA inteiros; não resolver ocultando conteúdo necessário.
2. **Floor / estados e STORY:** separar visualmente foco/hover de valor escolhido. Manter fundo+sublinhado como seleção e usar um tratamento de foco distinto, como contorno fino, sem dar à opção não selecionada um bloco mais luminoso que a opção ativa. Validar modo local com foco em Contra IA e modo IA com foco em opção de lado/dificuldade diferente da escolhida; foco deve continuar visível pelo teclado.

## keep

Preservar os dois modelos reais, a base de cada lado, a troca de destaque e nome, a marca Feevale proporcional, o campo verde profundo, a chamada amarela e a separação clara entre apresentação e configuração; não restaurar o tabuleiro amarelo nem acrescentar motivos do card sem decisão do usuário.
