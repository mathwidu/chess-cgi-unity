---
status: accepted
date: 2026-10-28
authored-by: agent
---

# Preservar nitidez no perfil Web

## Context and Problem Statement

O primeiro teste em iPhone físico foi rejeitado pela imagem pixelada e pela
aparência dos personagens. O modelo do aparelho e o ponto exato da experiência
ainda não foram identificados. No código anterior, o canvas limitava a densidade
a 1,25 e o perfil Web começava com renderScale 0,85 em mobile. Quatro amostras
consecutivas abaixo de 26 FPS reduziam a escala em 0,1 até 0,65, sem recuperação.
Essa redução também podia ocorrer no desktop. FPS agregado não identifica se
o custo vem da GPU, CPU, compilação ou outra atividade do navegador.

## Considered Options

- Manter a redução automática de resolução a partir de FPS agregado.
- Manter a resolução da cena estável e observar o desempenho sem descartar detalhe.
- Alterar simultaneamente texturas, iluminação, antisserrilhamento e personagens.

## Decision Outcome

O clone de pipeline criado por `WebRuntimeProfile` usa renderScale 1 em mobile e
desktop Web. As amostras de FPS deixam de alterar a resolução. O diagnóstico
ativado por `qa=1` mantém FPS, escala e tamanho da tela e acrescenta intervalo
da amostra e foco para interpretar a medição.

O perfil Web continua solicitando MSAA 2×; isso não comprova o número efetivo
de amostras suportado por um iPhone. O modo desempenho permanece uma escolha
explícita do jogador, com a mesma preferência do navegador e as mesmas peças
clássicas. Peças personalizadas, texturas, iluminação, sombras e render textures
dos previews ficam com suas configurações existentes nesta correção.

A densidade do canvas é controlada pelo launcher e pelo template Web: até dois
pixels por ponto CSS, respeitando a densidade do aparelho e um teto de três
milhões de pixels. O buffer acompanha rotação e tela cheia, sem alterar a
proporção. Essa política usa `matchWebGLToCanvasSize: false`, conforme o controle
manual documentado pelo Unity 6000.3. Escala 1 no Unity elimina a segunda redução,
mas não resolve sozinha texturas insuficientes em um close-up. O perfil é uma
cópia temporária; o asset fonte permanece intacto e o pipeline anterior volta
quando o componente é destruído.

### Consequences

O teste `WebRuntimeProfileTests.SustainedSlowSamplesKeepCharacterRenderResolutionStable`
usa o componente real e o perfil Web, fornece amostras controladas de lentidão
e verifica a resolução, o MSAA solicitado, a preservação do asset e a restauração
do pipeline. Na etapa vermelha, executada pelo principal, falhou na quarta
amostra porque a escala caiu de 1 para aproximadamente 0,9. O teste não exige
que o dispositivo atinja um FPS específico.

Manter mais pixels pode aumentar o custo da GPU. A etapa verde passou junto aos
testes de layout e sessão (13/13 em WebGL, 28/10/2026). O novo build, a comparação
visual e a fluidez no iPhone físico seguem como verificações da integração.
A mudança de política não estabelece compatibilidade nem aceite no aparelho.
Nenhuma publicação decorre desta decisão.
