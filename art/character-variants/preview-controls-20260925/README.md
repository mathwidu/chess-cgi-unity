# Preview da peça selecionada — 25/09/2026

O painel da peça agora explica o arraste e oferece controles visíveis para
girar nos dois sentidos, afastar, aproximar e restaurar a vista. O arraste
horizontal gira o personagem; o vertical muda o ângulo de observação entre
−15° e 65°. O zoom mostra o percentual e desativa o botão ao atingir o limite.
A vista inicial de cada personagem é 100%; Restaurar recupera posição,
orientação e distância. Trocar de peça inicia um novo enquadramento.

Nome, tipo, lado e casa ficam no cabeçalho. Categoria e registro ficam no
rodapé, sem repetir o nome. Os botões não cobrem o personagem. A textura de
1104 × 660 tem a mesma proporção da imagem de 368 × 220 unidades do Canvas.
O enquadramento considera os oito cantos dos limites 3D, reservando margem
para bases e acessórios.

## Verificação

- 56/56 testes PlayMode passaram, incluindo seis novos testes do preview.
- Arraste e roda foram exercitados com eventos do Input System, verificando
  que a seleção, as peças e a câmera do tabuleiro não se movem.
- Botões de giro, limites de zoom, restauração e reconstrução do HUD testados.
- Enquadramento dos seis personagens dos dois times conferido por projeção.
- Controles no Canvas World Space conferidos com XRHMD simulado.
- Quatro capturas da Main em 1920 × 1080 inspecionadas: peão, giro/zoom,
  rei e cavalo. O capturador usa Screen Space Camera para incluir o HUD no
  render; o jogo desktop continua em Screen Space Overlay.
- 185 arquivos protegidos mantiveram seus hashes, incluindo personagens,
  ambiente, cena Main, câmeras de jogo, entrada e configurações XR.
- Domainbook validado. Relatório detalhado: `verification.json`.

A Unity foi reaberta na Main em Play Mode, no menu. A automação nativa não
conseguiu clicar na janela Game nessa conferência final; a verificação de
mouse vem dos testes no Editor, sem declarar uma passagem manual inexistente.
Headset físico permanece pendente.

## Reprodução e recuperação

`PiecePanelReviewCapture.Run` gera estas capturas no Editor em batch, com
nenhuma outra instância aberta no projeto. Não salva cenas nem preferências.
Backups dos três scripts de produção e dos documentos alterados estão em
`.local/preview-controls-20260925/before/`. O manifesto `preserved.json` fica
um nível acima. Os assets dos personagens e da sala não foram alterados.
