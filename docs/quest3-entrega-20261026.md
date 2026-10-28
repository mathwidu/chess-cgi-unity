# Versão estável para entrega — Quest 3 — 26/10/2026

Abra **Xadrez CGI** na grade de aplicativos do Quest, no filtro
**Fontes desconhecidas / Unknown Sources**. O jogo instalado funciona sem
cabo e sem o Mac. A versão **0.1.3 (4)** foi escolhida pelo usuário como estável para
a exposição, após aprovar aparência, movimento e jogabilidade. O mesmo
APK aceito permanece instalado, com o nome e identificador do jogo anterior.

## Identificação e uso no headset

| Ação | Resultado esperado |
| --- | --- |
| Conferir o rodapé do menu. | **v0.1.3 (4) · 26/10/2026 17:39:37 Brasília (UTC-3)**. |
| Desligar **Modo desempenho** para conferir os personagens. | Os modelos da turma aparecem; a comparação temporária com peças clássicas foi encerrada. |
| Com mãos livres, escolher **Dois jogadores** e **Jogar** pela pinça. | Um clique por gesto; o tabuleiro aparece. |
| Fazer alguns lances legais alternando as mãos. Abrir a pinça lentamente a cerca de 20–30 cm acima da casa verde e deixar cair. Fazer também uma captura soltando acima da adversária. | A peça pousa, encaixa e muda o turno uma única vez. Não volta à origem. |
| Fechar uma mão em uma haste; depois fechar a outra na segunda haste. Afastar e aproximar as mãos e abrir para soltar. | Uma mão sozinha não redimensiona; duas ajustam o tamanho. Soltar mantém o tamanho escolhido. A pinça também continua disponível. |
| Segurar uma peça e passar a outra mão perto das vizinhas; soltar a peça e afastar os dedos. | As vizinhas ficam protegidas durante a pega. Depois, a mão aberta volta a empurrar. |
| Soltar uma peça na palma aberta, ao lado do tabuleiro, e mover a palma devagar. | A peça fica apoiada e nenhum lance é registrado. |
| Trocar de mãos livres para controles e voltar; abrir as mãos antes de agarrar outra vez. Voltar ao menu e iniciar outra partida. | Não há haste presa, seleção espontânea, clique involuntário ou peça antiga na nova partida. |
| Iniciar **Contra IA**, fazer alguns lances, voltar ao menu e começar novamente. | A IA responde e a nova partida inicia normalmente. |
| Olhar para o tabuleiro com os 32 personagens e mover a cabeça por cerca de 30 segundos. | Aparência e movimento aprovados pelo usuário; MSAA 4× e escala 0,80. A coleta mediu 65,5 FPS (63–68), abaixo da meta técnica de 72 Hz. |

O contorno verde indica uma **casa legal**. O lance por mãos exige uma
soltura lenta e pouso real na mesma casa, sem apoio da mão. A altura,
sozinha, não recusa a tentativa; a janela inclui tempo para a queda, com
limite de 2 segundos. Durante essa observação, a peça pode passar pela
adversária para concluir uma captura. Arremessar ou carregar na palma
continua sendo interação física, sem lance. Uma soltura inválida pode retornar à origem após o tempo de
recuperação.

## Estado da verificação

- Versão **0.1.3 / código Android 4**, instalada e iniciada em 26/10 às
  17h42 (São Paulo). Hash do arquivo no aparelho conferido novamente após
  o aceite: `541bf61c8f198f504778035a0d0c4b62be4690709c6030a387bace9aed08532e`.
  Há uma instalação de `com.mathwidu.chesscgi`. Versão/código vieram do
  manifesto instalado e a data do recurso incluído no APK.
- O usuário aprovou aparência no tabuleiro e de perto, movimento fluido
  para ele e jogo normal com as mãos. Em seguida, pediu explicitamente
  fixar esta versão como estável para a entrega. O aceite não envolve uma
  recompilação: o APK é exatamente o que foi testado.
- **278/278 PlayMode e 70/70 EditMode**, zero falhas/skips, com os mesmos
  fontes C# do APK. Incluem colocação, captura, menu, hastes, apoio na mão,
  clones, contornos e troca de detalhe. Os 41 fontes de controles, regras,
  IA e física conferidos permanecem iguais aos da comparação 0.1.2 aceita.
- Personagens usam **150.720 triângulos** quando todos estão no detalhe de
  distância; mais perto ou durante hover/pega usam inspeção. Texturas,
  materiais, limites originais e colliders preservados. O preview usa
  inspeção e reutiliza sua imagem parada.
- Coleta de 30 s olhando para os 32 personagens: **65,5 FPS de mediana**,
  mínimo 63, máximo 68, GPU 98%. Melhorou de 56 FPS; continua abaixo de
  72 Hz. O usuário escolheu a versão após relatar conforto e jogo normal.
  Esse aceite não comprova desempenho sustentado a 72 FPS.
- MSAA **4×**, escala **0,80**, HDR e sombras em tempo real desligados,
  foveation **0,5**. A consulta Vulkan mostrou suporte a 1/2/4× no
  framebuffer; 8× não está disponível nessa configuração.
- Fontes Blender reabertos e GLBs exatos reimportados; UVs/normais/tangentes
  verificados. A revisão independente da adaptação terminou sem achados
  acionáveis. [Condições e resultados](quest3-performance-20261026.md).

## Arquivos e recuperação

- APK estável: `Builds/Quest3/stable-20261026-v0.1.3/XadrezCGI-Quest3-stable.apk`.
- Identificação: [`quest3-stable.json`](quest3-stable.json).
- Fontes correspondentes: `source-changes.tar.gz`, `tracked-changes.patch`
  e `checkpoint.json` na mesma pasta do APK. O checkpoint registra commit
  base, branch, arquivos alterados e hashes. Aplicar o snapshot sobre uma
  cópia separada do commit registrado para recuperar o estado sem afetar
  o checkout atual. Estudos de animação alheios não fazem parte do snapshot.
- Licença e código-fonte correspondente do Stockfish acompanham o APK.
- Evidências: `.local/quest3/performance-detail-20261026/`.
- Comparação 0.1.2 / escala 0,80: `Builds/Quest3/comparison-20261026-scale080/`.
- Versões anteriores preservadas: `delivery-20261026-v0.1.2/`,
  `delivery-20261026-v0.1.1/`, `working-20261023/` e `stable-20261022/`
  sob `Builds/Quest3/`.

Para reinstalar esta versão estável no mesmo Quest, usar o APK preservado
com `adb install -r`, conservando os dados e a assinatura. A versão estável
usa código Android 4; APKs anteriores usam códigos menores e uma instalação
com `-r` não garante downgrade. Para recuperar uma lógica mais antiga,
recompilar seus fontes preservados com código compatível e a mesma
assinatura. Não desinstalar o jogo nem alterar contas como atalho.
