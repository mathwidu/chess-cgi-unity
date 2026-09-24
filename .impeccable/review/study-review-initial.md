disposition: fix

Substituição declarada: revisão independente nova pelo roteiro local `finish-reviewer.md` e `craft-floor.md`, em lugar do agente Impeccable nomeado; não foi fornecido um cartão QUALITY BAR. Revisão visual em Unity uGUI, sem detector HTML/CSS e sem certificação de hardware VR.

## persistence

**Parcial; encerramento ainda pendente.** `PRODUCT.md`, contrato, imagem aprovada, sidecar com `approved: true`, `build/state.json` e `build/spec.json` existem. A escolha explícita “essa daqui” e a tradução para UI nativa com os dois prefabs reais estão registradas. O contrato contém `FORM` com seed `9d5682be`; a composição posterior escolhida pelo usuário é a autoridade desta revisão. A abertura do contrato ainda diz “Não há comp aprovado”, enquanto sua seção final registra a aprovação: reconciliar no fechamento documental.

O estado lido nesta rodada tinha `comps: skipped` com justificativa de aprovação prévia, `spec: open` e `plates/hero: pending`. O executor informou que concluirá esses registros com a tradução nativa e fará o fechamento documental depois desta revisão. `DESIGN.md` e `design.json` ainda descrevem a versão anterior; são pendências de entrega, não fundamento para rejeitar a composição atual. O relatório de comparação disponível é `diff/study/report.json`, com 0,8903 e veredito mecânico `match`; os diretórios convencionais `diff/hero` e `diff/final` ainda não estavam presentes. A pontuação não substitui esta inspeção.

As doze capturas exigidas foram abertas: `desktop` 1672×941, `laptop` 1280×800, `compact` 1024×768, `wide` 2560×1080, `editor` 1223×704, e `white-beginner`, `local`, `help`, `match`, `selection`, `promotion`, `error` em 1920×1080. Elas correspondem aos estados nomeados, sem render quebrado ou viewport omitido no conjunto solicitado. As faixas verde-escuras dos formatos diferentes são a continuação intencional do campo da composição, não captura preta inválida. `editor.png` contém o jogo renderizado, não a interface do Editor. As evidências são estáticas: não comprovam animação, navegação integral, resultados dos testes ou experiência no headset.

## fidelity

Inventário feito primeiro sobre a imagem aprovada: título branco grande com CGI amarelo e subtítulo à esquerda; assinatura Feevale sobre o eixo do painel; planta, livros e peças desfocadas; piso de tabuleiro em perspectiva; Marta menor à esquerda e Ricardo maior ao centro no estado pretas; bases e nomes integrados ao piso sombreado; painel à direita com três grupos, confirmação amarela, ação Jogar volumétrica e ajuda abaixo; crédito discreto na base.

| Elemento | Estado | Evidência e limite |
| --- | --- | --- |
| Topologia, escala focal e ordem de leitura | match | Palco à esquerda, configuração à direita e ação dentro do painel reproduzem a composição. Nenhum controle principal foi omitido. |
| TYPE — caráter e hierarquia | adaptation | Lato incorporada mantém a voz sans humanista, título pesado e degraus claros; a alternativa redistribuível à referência Avenir está registrada em `DESIGN.md`. O diff encontra deriva no título e em “Nova partida” (279×46 contra 308×50 de tinta), mas não uma troca de caráter ou hierarquia. |
| MATERIAL — cenário e professores | adaptation | O fundo continua raster cenográfico, enquanto os personagens são os prefabs reais renderizados. Essa diferença é exigida pela seção de aprovação do contrato; rostos, roupa e malha próprios não devem ser substituídos por ilustração de IA. A logo original também é uma adaptação correta à verdade do produto. |
| GROUND — valor e temperatura do campo | match | Amostras medianas 5×5 em (900,80): comp RGB 15/41/33; desktop 12/40/33. Em (900,150): 12/38/31 contra 9/37/31. Mantém o verde profundo, sem deriva material de temperatura. O piso claro sob as legendas é tratado abaixo. |
| Logo Feevale e título do projeto | match | Arte visível da logo centralizada acima do painel, com proporção preservada e separada do título. Título, subtítulo e CGI permanecem alinhados como grupo. |
| Cenografia: livros, planta e tabuleiro | match | O plate está visível e sustenta o mundo aprovado. O código usa `Resources.Load("UI/MenuStudyBackground")`; o caminho corresponde à região raster da spec. Não há substituição por geometria de interface. |
| Alternância dos dois professores | adaptation | `desktop` mostra Ricardo maior; `white-beginner` mostra Marta maior; `local` iguala os dois. É a adaptação dinâmica pedida pelo usuário, preservando os modelos próprios. A fluidez da transição não é comprovada por imagens estáticas. |
| Nomes, subtítulo e sombra junto às bases | contradicted | O posicionamento acompanha as bases, mas os nomes tocam visualmente a borda inferior e caem em casas claras. Na comp, a região sob os nomes é escura e tem mais respiro. O contraste insuficiente se repete em desktop, laptop, compacto e local. Ver F1. |
| Configuração, confirmação e distinção de seleção | match | Modo, lado e dificuldade aparecem imediatamente; borda e confirmação amarela persistem. Em `local`, o contorno claro no primeiro modo e a confirmação no segundo distinguem foco de seleção. |
| Dificuldade no compacto | contradicted | `compact.png` com Difícil selecionado não apresenta recorte aparente. O executor informou falha específica do teste com Intermediário selecionado em 1024×768; essa variante não foi visualmente comprovada nesta rodada. Ver F2 antes de fechar a cobertura. |
| Jogar e Como jogar | adaptation | A geometria, amarelo e faixa inferior de profundidade preservam a ação dominante. A tonalidade mais uniforme do uGUI explica parte do `drift` mecânico de Jogar sem substituir sua função visual. A indicação de foco de Jogar precisa ser corrigida ou demonstrada: ver F3. |
| Enquadramento responsivo | adaptation | Nos cinco tamanhos de menu, logo, personagens, painel e crédito cabem sem interseções novas. O ajuste proporcional com campo verde ao redor preserva a composição escolhida e a leitura essencial; não comprova tamanhos arbitrários ou leitura angular em VR. |
| Ajuda, erro e promoção | match | Textos e ações estão legíveis nas capturas fornecidas. O erro oferece recuperação explícita. O escurecimento de fundo preserva o foco do diálogo. Os números na ajuda expressam uma sequência real; CÂMERA/PARTIDA são categorias funcionais. |
| HUD de partida e peça selecionada | match | As capturas mostram tabuleiro, ações, turno, histórico vazio e painel de identificação sem quebra gráfica causada pelo menu. Não certificam regras, dados cadastrais ou comportamento funcional. |
| Crédito acadêmico | adaptation | Presente no rodapé, mais discreto que a comp, coerente com a natureza acadêmica registrada em `PRODUCT.md`. |

O teste de memória visual mantém as promessas `THESIS`, `OWN-WORLD`, `STORY` e `FIRST VIEWPORT`: os professores e a identidade Feevale são reconhecíveis antes de configurar e jogar. `FORM` está subordinada à composição explícita posterior; não justifica reintroduzir motivos da direção anterior. Nenhuma substituição global da página ou reconstrução dos modelos é necessária.

## ceiling

O cartão QUALITY BAR específico não foi fornecido; não declaro seu teto atingido. A referência aprovada exige integração mais convincente entre bases, sombras e nomes: hoje o piso claro reduz tanto a leitura quanto o assentamento dos personagens. Esse é o recurso de profundidade que ainda precisa de acabamento. O campo, a cenografia, a escala dos personagens e a ação principal já estão comprometidos com o mundo escolhido. As capturas não justificam aumentar ornamentação, criar cartões para os professores, trocar seus rostos ou redesenhar o restante da partida.

A amostra de `MenuSurface.cs`/`MenuGroundShadow.cs` e as imagens mostram gráficos uGUI separados do plate, sem clipping novo visível no conjunto fornecido. Compatibilidade de máscara, raycast, Canvas world-space e comportamento XR pertencem aos testes nativos do executor; não foram aprovados por esta inspeção visual. Nenhum segundo detector foi executado.

## material_fixes

| Ordem | Prioridade | Correção e critério de encerramento |
| --- | --- | --- |
| F1 | P1 | **Fidelidade / contraste / FIRST VIEWPORT:** dar aos nomes e ao subtítulo uma região escura suave, integrada à sombra do palco, e respiro sob as bases; preservar sua associação projetada aos professores. Não criar placas/cartões. Branco contra o piso local medido em desktop resulta em cerca de 1,60:1 em (310,689) e 1,90:1 em (610,779); no compacto, cerca de 1,82:1 em (185,519). Na comp, os dois pontos desktop correspondentes superam 11:1. A sombra de texto deslocada não resolve o lado claro dos glifos. Validar texto comum ≥4,5:1 em pretas, brancas e local, inclusive 1024×768, sem diluir a cenografia. |
| F2 | P1 | **Floor / responsive / seleção:** corrigir o espaço de Intermediário selecionado em 1024×768, falha comunicada pelo executor; manter rótulo em uma linha, confirmação separada e largura total do grupo dentro do painel. Entregar teste aprovado e captura dessa variante. A imagem atual com Difícil selecionado não prova a correção. |
| F3 | P1 | **Floor / foco acessível:** conferir e corrigir a indicação de foco de Jogar. `StyleMenuButton` define `normalColor` e `selectedColor` como `Color.white` e é aplicado ao botão; o `FocusRing` explícito aparece apenas em `CreateMenuChoice`. A amostra não demonstra outro indicador de foco para Jogar. Se houver mecanismo adicional, apresentar a evidência; caso contrário, aplicar contorno independente e capturar Jogar focado, distinguível do repouso e de hover. |

## keep

Preservar a composição aprovada, a logo original centralizada acima do painel, os dois prefabs reais com destaque por lado, nomes junto às bases, confirmações amarelas, palco cenográfico e controles uGUI nativos.
