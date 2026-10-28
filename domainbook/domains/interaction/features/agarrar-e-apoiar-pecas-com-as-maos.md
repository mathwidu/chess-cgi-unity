---
id: agarrar-e-apoiar-pecas-com-as-maos
name: Agarrar e apoiar peças com as mãos
status: draft
owners: [mathwidu]
terms: [agarrar-e-soltar, rastreamento-de-mãos]
---

## Story

Como jogador com mãos rastreadas no Quest
Quero pegar uma peça de perto e apoiar uma peça solta na palma aberta
Para interagir com o tabuleiro com gestos fáceis e contato físico previsível

## Rule: A pinça expressa a intenção de agarrar

```gherkin
Example: A pinça começa antes de alcançar a peça
  Given uma mão rastreada fora do alcance próximo
  When o jogador fecha a pinça e aproxima os dedos de uma peça permitida
  Then a mão adquire a peça sem exigir abrir e fechar os dedos novamente
  When o jogador abre a pinça
  Then a peça é solta pelas regras já existentes de lance e peça solta

Example: Encostar com a mão aberta
  When a mão aberta chega perto de uma peça ancorada no tabuleiro
  Then o contato pode empurrar e derrubar a peça e suas vizinhas
  And nenhuma peça é escolhida e nenhum lance é iniciado
  And a casa lógica, o turno e o histórico permanecem iguais

Example: Duas mãos pinçadas perto da mesma peça
  Given uma peça agarrada pela primeira mão
  When a segunda mão se aproxima mantendo uma pinça
  Then a primeira continua com a peça
  When a primeira abre os dedos
  Then a segunda pode adquirir a peça liberada

Example: Outra mão confirma um lance
  Given uma peça ainda segurada pela primeira mão
  When outra peça faz um lance e o turno muda
  Then a primeira mão mantém a posse física até abrir
  And soltá-la depois não registra outro lance

Example: Reiniciar enquanto segura uma peça
  Given uma peça temporariamente fora da hierarquia do tabuleiro por estar agarrada
  When uma nova partida reconstrói o jogo
  Then o objeto antigo é removido e a pega é cancelada
  And o novo conjunto tem exatamente as 32 peças iniciais

Example: A peça mais próxima não é permitida pela vez
  Given uma peça adversária mais próxima e uma peça permitida vizinha
  When o jogador pinça sobre a adversária
  Then a seleção é recusada sem atrair a peça vizinha
```

`XRRig.BuildHandInteractor` usa seleção por estado da pinça e um filtro de
posse. Os controles preservam sua seleção por novo aperto. O alcance próximo
continua em 6 cm, e os raios continuam reservados ao HUD. A força analógica
da pinça inicia a pega a partir de 0,75 e a mantém acima de 0,55. A leitura
usa exclusivamente `MetaAimHand.pinchStrengthIndex` da mão rastreada. O
`Select Value` composto também recebia `HandInteraction.graspValue`: fechar
os demais dedos mantinha a peça presa mesmo abrindo indicador e polegar.
O clique do HUD compartilha a leitura da pinça, com cancelamento próprio do
ponteiro para não converter perda de rastreamento em clique. Rastreamento
inválido, foco perdido e pausa soltam e desarmam a pega. Voltar com os dedos fechados exige abrir antes
de uma nova aquisição; essa saída involuntária nunca confirma uma jogada.

## Rule: A pinça aciona o menu sem exigir apertar mais os dedos

```gherkin
Example: Começar a partida com qualquer mão
  Given o raio da mão rastreada sobre um botão do menu
  When o jogador faz uma pinça deliberada e abre os dedos sobre o botão
  Then o botão recebe exatamente um clique
  And a mesma pinça permite escolher Dois jogadores e depois Jogar

Example: Interromper uma pressão no menu
  Given a pinça pressionando um botão do menu
  When o rastreamento, foco ou modo de mãos é interrompido, ou o jogo pausa
  Then a pressão do ponteiro é cancelada sem clicar
  When a mão volta ainda pinçada e depois abre
  Then apenas rearma a leitura
  And uma nova pinça pode clicar normalmente
```

`XRHandMenuInput` liga o clique ao mesmo sinal exclusivo de indicador e
polegar, com histerese 0,75/0,55. Antes de o módulo de UI processar uma mão
inválida, o componente suspende sua interação de UI pelo registro do XRI.
Isso cancela a pressão em vez de simular a soltura que dispara um clique.
Os controles continuam usando seu gatilho. O leitor anterior do exemplo XRI
exigia força superior a 0,9 mesmo quando o botão nativo já indicava pinça.

## Rule: Agarrar uma peça protege as vizinhas das duas mãos

```gherkin
Example: Transportar uma peça entre as outras
  Given uma mão segurando uma peça
  When a mão e a peça passam pelas vizinhas
  Then não as empurram nem derrubam
  And a outra mão também não empurra nem derruba peças

Example: Uma de duas pegas termina
  Given duas peças seguradas fisicamente
  When uma delas é solta ou removida
  Then as duas mãos continuam sem contato físico com peças
  When a última pega termina
  Then os contatos livres das duas mãos voltam a funcionar

Example: Abrir a pinça dentro de outra peça
  Given a mão ocupada sobreposta a uma vizinha
  When o jogador abre os dedos
  Then os contatos sobrepostos aguardam separação antes de voltar a colidir
  And uma palma livre pode apoiar a peça mesmo com um dedo ainda sobreposto
```

A existência de qualquer pega física suspende os contatos das duas mãos com
as peças. Esse estado acompanha a posse do XRI, inclusive desativação da
última peça, sem depender da seleção lógica que mostra destinos legais.
A peça agarrada também exclui colisões com suas vizinhas. Ao terminar a
última pega, cada mão restaura pares livres e aguarda separação nos sobrepostos;
não exige que todos os dedos saiam para reativar a palma. A peça restaura
suas colisões quando todo o volume está livre, incluindo o topo de peças
altas e ampliadas. Esse estado não substitui o rearme global após perda de
rastreamento. A supressão invalida apoios antigos e desperta peças próximas.

## Rule: Uma peça solta pode se apoiar na mão

```gherkin
Example: Repousar e transportar uma peça
  Given uma peça solta sob a gravidade e uma palma aberta rastreada
  When a peça cai sobre a palma
  Then o contato físico sustenta a peça
  And mover a palma suavemente transporta a peça por atrito
  And a peça continua independente da hierarquia da mão

Example: Um dedo sai enquanto a palma ainda apoia
  Given contatos simultâneos da palma e de um dedo com a peça
  When o dedo deixa o contato
  Then o apoio restante da palma continua suspendendo a recuperação

Example: Perda e retorno do rastreamento
  Given uma peça repousando sobre a mão
  When a mão perde rastreamento ou é desativada
  Then o contato some e a peça desperta para voltar a cair
  When a mão ou um segmento de dedo reaparece dentro de uma peça solta
  Then o contato aguarda estabilidade e espaço livre antes de reativar
```

`XRPhysicalHand` mantém os 18 contatos existentes dedicados aos botões e cria
um `XRHandContactBody` separado, com um corpo cinemático, uma caixa para a
palma e 15 cápsulas para os segmentos dos dedos. O corpo usa poses originais
das articulações, sem realimentação do deslocamento visual dos botões.
A palma se orienta pela geometria de punho e nós dos dedos, em ambas as mãos.
As formas acompanham a física; as peças soltas mantêm gravidade e atrito.

O corpo consulta apenas a camada das peças (6). Em repouso as peças são
dinâmicas adormecidas: podem despertar por uma mão ou outra peça, inclusive
as adversárias, sem alterar o estado de xadrez. A pega continua respeitando
a vez. Durante a pega e a animação de um lance o corpo é cinemático. A mão
usa a camada 28, excluída dos alvos de agarrar e dos raios do HUD. A caixa
da palma mantém 29 mm de espessura. A margem adicional de 0,5 mm em cada
face corrige a pequena penetração estática medida na base visual do peão.
Durante apoio válido, o desenho da mão acompanha a posição e a rotação
interpoladas do corpo físico, com limites de 2 cm no centro e 10°.
A transformação parte da pose crua salva, sem acumular correções entre callbacks.
Isso reduz a diferença temporal entre pele e peça sem modificar
poses cruas, seleção, arremesso ou corpos físicos. A restrição visual da
face dos botões avalia cada ponto já transformado e soma sua correção depois.
Não há calibração da malha em runtime nem alteração da frequência física.
Perda de rastreamento, foco, pausa e mudança da origem desarmam os contatos;
o retorno exige três passos estáveis e ausência de peças dinâmicas no volume
proposto. Um segmento isolado também tem essa proteção ao recuperar um joint.
As consultas de espaço livre ocorrem durante o rearme, com buffers reutilizados.

`ThrowablePiece` distingue apoio contra a gravidade de qualquer contato
válido da mão, por collider. Um dedo saindo não anula o apoio da palma; o
contato lateral também impede a recuperação atravessar uma mão que ainda
está empurrando. Uma revisão invalida contatos antigos quando a mão ou um
segmento é desarmado, mesmo sem evento de saída. Após três segundos sem
contato da mão, a peça volta à origem e adormece sob física. Agarrar de novo
cancela a recuperação. Massa e centro de massa baixo são definidos antes
do primeiro toque, sem exigir uma pega anterior.

## Rule: Uma soltura intencional confirma o lance quando a peça pousa

```gherkin
Example: Lançar por cima de um destino legal
  Given uma peça agarrada sobre um destino legal
  When o jogador solta com velocidade de arremesso
  Then a seleção é cancelada sem lance
  And a peça continua sob a física para ser pega novamente

Example: Soltar suavemente sobre a casa destacada
  Given uma peça da vez agarrada acima de uma casa legal destacada
  When o jogador abre os dedos sobre um destino legal em baixa velocidade
  Then a peça se solta imediatamente e pode descer até o tabuleiro
  And o lance só é confirmado após contato real sem apoio da mão
  And a casa deve continuar sendo o mesmo destino legal da soltura

Example: Um dedo ainda apoia a peça ao abrir a pinça
  Given uma colocação lenta iniciada perto da superfície sobre uma casa legal
  When o dedo sai e a peça chega ao tabuleiro dentro da janela de observação
  Then a jogada é julgada pelas regras existentes uma única vez
  But continuar carregando a peça na palma não confirma um lance

Example: Interromper uma colocação antes do pouso
  Given uma peça solta observada sobre um destino legal
  When o aplicativo perde foco ou é pausado
  Then a observação e os destaques são cancelados imediatamente
  And retomar o aplicativo não confirma o pouso antigo
  When a peça é agarrada novamente durante a espera
  Then a nova pega encerra a tentativa de colocação anterior

Example: Outra peça termina um lance
  Given uma peça solta fora de sua casa lógica
  When outra peça confirma um lance
  Then o objeto solto e seu movimento físico são preservados
  And captura, promoção e reinício ainda removem ou recriam as peças afetadas
```

A ponte de interação guarda posição e validade no instante da saída e
espera o detach do XRI. Para mãos rastreadas, a altura acima da casa não
recusa por si só uma soltura intencional. A observação dá 600 ms mais o tempo
estimado de queda pela gravidade, com prazo total de até 2 segundos. Assim
ela termina antes da recuperação automática de uma peça sem contato (3 s).
Confirmar exige contato físico recente com o tabuleiro, base a até 5 mm do
plano, ausência de apoio da mão e permanência na mesma casa legal da saída.
A espera não prende a peça nem a transporta para casas vizinhas. A proteção
entre peças permanece durante essa observação: uma captura solta acima da
adversária pode chegar ao tabuleiro sem empilhar ou derrubar as vizinhas.
Confirmar ou cancelar encerra essa extensão; pares ainda sobrepostos
continuam aguardando separação antes de retomar colisão.

Em 26/10, o usuário identificou a recusa ao abrir a pinça um pouco acima de
uma casa verde. Replays pelo rig e entrada nativa reproduziram a falha em
5 e 9 cm, mesmo com pouso real no destino, e quando o dedo levou 320 ms para
se afastar. A margem e a janela anteriores encerravam essas tentativas antes
de avaliar o pouso. O usuário voltou a pedir maior tolerância após testar o
candidato de 10 cm e enfatizou a confiança na jogada. Replays de 20, 50 e
120 cm reproduziram o mesmo problema: houve pouso, mas zero lances. A revisão
remove o teto de altura e mantém as condições físicas abaixo. Aceitação no
aparelho continua sendo uma verificação própria.

A velocidade recente da entrada, medida em cerca de 30 ms, precisa ser de
até 0,35 m/s. Na colocação intencional ainda válida, a ponte
reaplica essa velocidade ao corpo depois do detach para que amostras antigas
amplificadas de arremesso não arrastem uma peça cuja mão já parou. O valor
original do XRI permanece registrado. Essa assistência não se aplica a
arremesso rápido, apoio persistente, cancelamento ou nova pega.
Os controles conservam a verificação anterior. Foco, pausa e rastreamento inválidos
cancelam a seleção. Foco e pausa cancelam a observação no próprio callback,
sem depender de outro frame antes da suspensão. Desativar a ponte ou perder
o detach também encerra a espera; cancelar continua permitido com a partida
suspensa, sem liberar a execução de lances. A sincronização de um lance reaproveita peças que não
mudaram de identidade/casa; reinício, troca de modelos e retomada explícita
continuam com reconstrução completa. Ajustar a mesa ou o tamanho realinha e
adormece somente peças em repouso, preservando peças soltas e agarradas.

## Rule: A colocação mostra o destino da base e termina com encaixe curto

```gherkin
Example: Segurar a peça sobre uma casa
  Given uma peça fisicamente agarrada
  When a base fica sobre uma casa do tabuleiro
  Then o destino de colocação contorna a casa considerada pela validação
  And verde indica destino legal, amarelo indica origem e vermelho indica casa ilegal
  And o contorno não registra lance nem modifica a posição da peça

Example: Pousar e abrir os dedos
  Given uma soltura que atende às regras de colocação física
  When o lance é confirmado
  Then a peça centraliza e alinha com encaixe curto sem arco no ar
  And movimentos por clique e pelo computador conservam sua animação

Example: Cancelar uma promoção física
  Given um peão colocado na última fileira aguardando escolha
  When a promoção é cancelada
  Then o peão volta à casa lógica de origem sem registrar lance nem captura
  And a física é restaurada após o retorno
```

O [destino de colocação](../../presentation/glossary.md#destino-de-colocação)
mostra a projeção da peça. O destaque não altera a escala da raiz física durante a pega. O encaixe
confirmado dura no máximo 100 ms. Uma nova animação da mesma peça invalida
a anterior, inclusive quando a promoção é escolhida imediatamente. O retorno
de uma promoção cancelada bloqueia entrada por 150 ms e só restaura o corpo
se a peça continuar ativa. Interrupções que não permitem lance não são
reportadas como sucesso pela API de soltura.

## Open Questions

Replays PlayMode em `HandNearSelectionTests`, `HandNearEligibilityTests` e
`HandPiecePhysicsTests` exercitam o seletor nativo, gravidade, transporte,
apoio composto, recuperação e falhas de rastreamento. A regressão também
inclui controles, modalidades, botões, HUD e ajuste do tabuleiro: **62/62**
passaram em 22/10/2026 (`TestResults/Quest3Hands/hand-interaction-final.xml`).
O primeiro teste no Quest em 23/10 confirmou melhora parcial da pinça e
apoio na palma, mas apontou dificuldade de pegar/largar e penetração na skin.
O usuário escolheu poder empurrar e derrubar peças sem lance pelo toque. A
revisão seguinte passou em 165/165 PlayMode na suíte completa, seguida de
34/34 regressões de ciclo de vida após corrigir o reinício com peça agarrada.
Relatórios: `natural-hands-final.xml` e `reset-lifecycle-final.xml` em
`TestResults/Quest3Hands/`. A suíte EditMode passou 64/64 após a última
alteração de código. No reteste físico dessa revisão, o usuário confirmou
que empurrar funciona, mas pediu proteger vizinhas durante a pega. Ainda
relatou peça presa mesmo abrindo a pinça e pequeno afundamento na pele.

A rodada seguinte reproduziu o conflito dos dois providers nativos e a
penetração temporal de cerca de 5 mm no desenho com contato físico íntegro.
Os REDs ficam em `pinch-mixed-red.xml` e `protection-temporal-red.xml`.
`pinch-green-button-red.xml` também reproduziu uma composição visual que
permitia atravessar a face do botão durante apoio. A regressão final passou
**64/64 EditMode e 184/184 PlayMode** em
`hands-placement-full-EditMode.xml` e `hands-placement-full-PlayMode.xml`.
No reteste físico do candidato `7765154…`, o usuário confirmou soltura pela
pinça e proteção da mão ocupada. Ainda relatou recusa de lance sobre uma
casa legal e pequeno afundamento na palma; pediu que a outra mão também
deixe de mover vizinhas durante a pega. Essa revisão protege as duas mãos,
observa o pouso físico e corrige também a rotação entre fases. Ainda exige
novo aceite físico de colocação, apoio, arremesso e fluidez.

O replay `landing-lifecycle-red.xml` confirmou três seleções pendentes entre
13 casos: desativar a ponte, desativar o objeto de interação antes do cache
de detach e pausar entrada e partida juntas. A revisão cancela a espera
nesses caminhos. O teste de pausa foi reforçado para exigir limpeza no
próprio callback, sem aguardar outro frame. Perder rastreamento e agarrar
novamente antes de pousar também são exercitados por entrada nativa.
A suíte final passou **199/199 PlayMode e 64/64 EditMode** em
`shared-landing-full-PlayMode.xml` e `shared-landing-full-EditMode.xml`,
sem mudanças de fontes ou testes durante a execução. A revisão independente
dos fontes não encontrou outros bloqueadores. No Quest com `7ff45c0…`, o
usuário confirmou proteção das duas mãos e apoio sem afundamento, mas o lance
continua recusado após soltar. Também relatou tremor no ar longe das peças.
Esses dois pontos mantêm o aceite parcial; o Editor não reproduziu ainda
o motivo da recusa ou a percepção de tremor do aparelho. Uma captura dirigida
no Quest deve separar movimento de entrada, altura, casa, contato, apoio,
rastreamento e apresentação de frames antes de mudar novamente os limites.
O APK de diagnóstico `1779faf…` foi usado nessa captura, com probes
temporárias por soltura e observação visual agregada. Seus comportamentos
de base passaram 40/40 regressões afetadas no Editor. Após o teste físico,
o caso A reteve 17 solturas: 12 ainda na casa de origem, duas fora do
tabuleiro, uma em destino ilegal e dois lances iniciados após contato.
Não houve recusa por velocidade ou prazo nesse trecho. A pinça crua aparece
por vezes distante da raiz da peça, exigindo investigar seguimento e base;
o motivo ainda não foi reproduzido no Editor. A mediana da sessão foi
37/72 FPS, com GPU próxima de 99%. Isso mantém apresentação irregular como
hipótese para o tremor; igualdade de raiz nativa/desenhada sem apoio não
prova conforto. Comparações de visual nativo e resolução temporária ainda
não foram executadas.

O refinamento seguinte reproduziu escala física transitória de 23,78 vezes
na seleção, arco de 18 cm após colocação aceita e cancelamento de promoção
que deixava o peão 79,55 mm fora da origem. A correção desses problemas,
contorno do destino, escolha imediata de promoção e animação de clique
passaram **209/209 PlayMode e 64/64 EditMode**. Dois replays de física foram
calibrados em 72 Hz e o arremesso passou a medir a velocidade de entrada;
os relatórios anteriores com falha foram preservados. O primeiro EditMode
teve uma falha de prazo no processo simulado; repetido sem mudar fontes,
passou completo. Essa evidência ainda não demonstra o fim da recusa
intermitente no aparelho. O próximo teste físico foi limitado pelo usuário
à colocação; a comparação de fluidez permanece adiada.

Essa evidência do Editor não mede precisão, facilidade, conforto ou custo
no Quest. O aceite físico da versão de demonstração anterior não se estende
automaticamente a esta nova interação.

No reteste de 23/10 do candidato `4751f26e…`, o usuário confirmou funcionamento
do menu por pinça e do contorno/colocação do peão. O leitor antigo do menu
exigia força acima de 0,9; a regressão reproduziu o botão sem clique nas duas
mãos a 0,85 e passou após compartilhar a leitura da pega. A rodada conjunta
passou 64/64 PlayMode, incluindo cancelamento de pressão por rastreamento,
foco, pausa e modalidade. A captura parcial do aparelho também reteve dois
lances iniciados após contato com o tabuleiro. Isso confirma o teste pedido,
sem concluir precisão de todas as jogadas, arremesso ou fluidez prolongada.
O tremor e a comparação de desempenho continuam em aberto. Os diagnósticos
temporários foram arquivados e removidos para gerar a versão normal de trabalho.
A suíte completa dessa versão passou 218/218 PlayMode e 64/64 EditMode,
com os mesmos fontes mantidos até o build.
