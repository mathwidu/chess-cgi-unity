# Glossário de interação

As palavras que o contexto de interação usa para a entrada e a câmera. Um
termo é um heading H2 com sua definição logo abaixo.

## Hastes de ajuste

Duas hastes metálicas presas aos lados do tabuleiro no modo VR. Segurar uma
com cada mão e afastar ou aproximar as mãos ajusta o tamanho em tempo real.
Uma haste sozinha e o raio do HUD não redimensionam o jogo; soltar uma mão
mantém o tamanho escolhido.
Com mãos rastreadas, a pega aceita pinça ou fechamento dos quatro dedos
sobre a haste. O fechamento usa a palma e só seleciona hastes; peças e HUD
continuam usando a pinça. Interromper o rastreamento exige abrir antes de
agarrar novamente.

- **Aliases:** BoardScaleHandles, Hastes laterais, Ajuste com duas mãos
- **Status:** draft

## Seleção

A peça que um jogador escolheu para jogar. Escolher uma peça mostra seus
destinos legais; escolher um deles, ou pressionar `Esc`, encerra a seleção.

- **Aliases:** Selection, Peça selecionada
- **Status:** validated

## Perspectiva

O lado para o qual a câmera principal está voltada. Ela acompanha o turno,
girando para o jogador a jogar, de modo que cada um lê o tabuleiro a partir
do seu próprio lado.

- **Aliases:** Perspective, Câmera por turno, Per-turn camera
- **Status:** validated

## Órbita

Girar a câmera principal ao redor do tabuleiro com `Q` e `E`,
independentemente de quem está jogando.

- **Aliases:** Orbit, Girar câmera
- **Status:** validated

## Olhar ao redor

Girar a visão a partir do assento atual para observar a sala no desktop,
mantendo a posição da câmera. O botão direito controla a direção; R ou o
botão de retorno restaura a perspectiva do tabuleiro.

- **Aliases:** Room view, Visão da sala
- **Status:** validated

## Modo VR

O jogo renderizado para um headset e controlado com controles de movimento ou
mãos rastreadas. Mantém o jogador na sala virtual do jogo. A interação está
implementada e foi exercitada no Editor; o port Android e a validação no
Meta Quest 3 estão em andamento.

- **Aliases:** VR mode, Immersive mode
- **Status:** draft

## Realidade mista

Modo futuro no Quest que apresenta o tabuleiro e os personagens sobre o
ambiente real visto por passthrough. Será uma opção adicional ao modo VR com
a sala virtual. Ainda não implementado nem validado no aparelho.

- **Aliases:** Mixed reality, MR, Realidade aumentada, Passthrough
- **Status:** draft

## Óculos VR

O display montado na cabeça que o jogador usa no modo VR. Sua pose rastreada
comanda a câmera, então a visão acompanha para onde o jogador olha, em vez de
girar para o lado a jogar.

- **Aliases:** Headset, HMD
- **Status:** draft

## Controle de movimento

Um controle de mão rastreado que o jogador segura no modo VR, usado para
agarrar peças e apontar para o HUD no lugar do mouse.

- **Aliases:** Motion controller, Controle, Hand controller
- **Status:** draft

## Raio de seleção

Um raio que um controle de movimento ou uma mão rastreada projeta e que só
aparece ao apontar para o painel do HUD. Os controles clicam com o gatilho;
as mãos usam a pinça de indicador e polegar, com o mesmo limiar da pega.
Abrir a pinça sobre o botão conclui o clique. Perda de rastreamento, foco,
pausa ou troca de modalidade cancelam a pressão pendente e exigem abrir
antes de uma nova pinça. Sobre um botão
ele aparece com brilho normal; com a mão vazia e apontando para o painel (com
uma folga de 150 unidades em volta), aparece mais escuro, para orientar a
mira. Não escolhe peças: elas são [agarradas e soltas](#agarrar-e-soltar) com
a mão. Tabuleiro, peças e mesa barram o raio, então apontar para eles nunca
aciona o HUD que fica atrás nem mostra o raio ocioso.

- **Aliases:** Ray interactor, Pointer ray
- **Status:** draft

## Agarrar e soltar

Escolher e jogar uma peça no modo VR com a mão, em vez de apontar. Segurar o
gatilho do indicador perto de uma peça da vez a agarra (e seleciona, mostrando seus destinos
legais); soltá-la devagar sobre um destino legal inicia uma colocação. Com
mãos rastreadas, o lance é confirmado ao pousar nessa casa, mesmo quando a
pinça abre mais acima. Arremessar mantém a peça sob a física sem lance. Soltar em qualquer
outro lugar, inclusive fora do tabuleiro, é jogada inválida; soltar na própria
casa apenas desfaz a seleção. Nesses dois casos a peça fica solta: cai, quica
e pode ser arremessada como uma [peça solta](../presentation/glossary.md), e
só volta à casa de origem depois de 3 segundos sem contato da mão. O controle
vibra de leve ao agarrar e ao soltar num lance aceito, e vibra mais forte e
mais longo quando a jogada é recusada.

Com mãos rastreadas, fechar a pinça um pouco antes de alcançar a peça também
permite agarrá-la ao entrar no alcance próximo. A pinça usa somente o sinal
de indicador e polegar, para os outros dedos fechados não prenderem a peça.
A existência de qualquer pega física suspende o contato das duas mãos com
peças; a peça segurada também atravessa as vizinhas sem derrubá-las. Após a
última soltura, pares livres voltam a colidir e pares sobrepostos aguardam
separação. Uma colocação lenta observa brevemente o pouso real na mesma casa
legal, sem aceitar apoio persistente na palma ou atrair uma casa vizinha.
Sem pega ativa, a mão aberta pode empurrar e derrubar peças;
esse contato não escolhe uma peça nem faz um lance. A casa lógica permanece
igual, mesmo quando a peça se desloca fisicamente. Uma segunda mão pinçada
aguarda a primeira soltar, sem disputar a peça. O alvo mais próximo é
preservado: uma peça proibida pela vez não atrai outra peça vizinha.
Uma peça solta pode repousar e ser transportada na palma aberta; enquanto
estiver em contato com a mão, a recuperação de três segundos aguarda sua
saída. Perda de rastreamento, foco e pausa desarmam a pinça e exigem abrir
a mão antes de agarrar novamente; não confirmam lance.

- **Aliases:** Grab and release, Pegar e soltar
- **Status:** draft

## Rastreamento de mãos

Uma forma alternativa de entrada no modo VR: o headset rastreia as mãos
livres do jogador, sem exigir que ele segure um controle de movimento. O
jogo alterna automaticamente entre controle e mãos, conforme qual dos dois
o runtime relata como presente.

- **Aliases:** Hand tracking, Mãos rastreadas
- **Status:** draft
