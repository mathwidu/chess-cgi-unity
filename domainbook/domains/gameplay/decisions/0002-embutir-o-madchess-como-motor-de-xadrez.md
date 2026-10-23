---
status: proposed
date: 2026-10-23
---

# Embutir o MadChess como motor de xadrez

## Context and Problem Statement

Jogadores reclamam que o nível Iniciante é forte demais. O Iniciante já usa
`Skill Level 0`, o mínimo do Stockfish 18, e o `UCI_Elo` mínimo (1320, escala
CCRL) corresponde ao mesmo nível. Mesmo ali o motor escolhe o lance na
profundidade 1 com busca de trocas completa e avaliação NNUE, sorteando entre
os quatro melhores lances com perda de no máximo cerca de um peão: ele quase
nunca entrega peças. Além disso, o Stockfish roda como processo separado, o
que não funciona no Quest standalone e, como medido, nem em builds IL2CPP de
desktop.

## Decision Drivers

- O nível mais fácil precisa ficar bem abaixo do mínimo do Stockfish e errar
  como um iniciante.
- O motor precisa rodar dentro do jogo em arm64 (Quest), sem processo filho.
- Rodar dentro do processo não pode impor a GPL ao jogo inteiro.
- Pensar não pode prejudicar o frame time.

## Considered Options

- Manter o Stockfish 18 e enfraquecer por conta própria, sorteando lances
  piores fora do motor.
- Fairy-Stockfish como processo (`Skill Level` até −20, como o nível 1 do
  Lichess): continua sendo processo separado, GPL, e a última versão oficial
  (2021) só tem binários x86-64.
- Patricia (C++, MIT, nível 1 ≈ 500 Elo pelo autor): exigiria adaptador C e
  builds nativos por plataforma.
- Maia (rede neural treinada em partidas humanas): pesos sem licença clara,
  piso de cerca de 1100 e suporte do Sentis no Quest não confirmado.
- MadChess (C#, MIT, `UCI_Elo` de 600 a 2600, com erros e lances entregues de
  propósito), portado para dentro do jogo.

## Decision Outcome

Opção escolhida: "MadChess portado para dentro do jogo". A v3.4 fica em
`game/Assets/ThirdParty/MadChess/`, gerada por `tools/port_madchess.py`
(C# 9 / .NET Standard 2.1, sem mudar a busca nem a avaliação), e
`MadChessMoveChooser` implementa `IMoveChooser` no lugar do Stockfish. Os
[níveis de dificuldade](../glossary.md) passam a um
[Elo de força limitada](../glossary.md): Iniciante 600, Intermediário 1200,
Difícil 2000, com os mesmos tempos por jogada. O adaptador do Stockfish
continua no repositório para comparação, mas a fábrica não o usa mais.

### Consequences

Medido em 2026-10-23 num Apple M5, cena `Main` real limitada a 90 fps, builds
macOS arm64 Mono e IL2CPP (`Builds/EngineBenchmark`, código em
`game/Assets/Benchmarks`):

- Bom, porque o Iniciante agora joga perto de 600 Elo pela escala do autor,
  bem abaixo do mínimo do Stockfish.
- Bom, porque roda em IL2CPP. O Stockfish não roda: `Process.Start` falha no
  player IL2CPP de macOS.
- Bom, porque não há impacto visível no frame time: p99 de 12,0 ms pensando,
  igual ao repouso, em todos os níveis e nos dois backends.
- Bom, porque a primeira jogada sai em cerca de tempo da jogada + 15 ms,
  contra tempo da jogada + 300 ms do Stockfish, que precisa iniciar o processo.
- Bom, porque usa menos memória: cerca de 25 MB de heap gerenciado (16 MB de
  hash), contra cerca de 420 MB de RSS do processo do Stockfish.
- Neutro, porque a CPU é a mesma: os dois motores ocupam um núcleo inteiro
  durante o tempo da jogada. Nos níveis baixos o MadChess gasta esse tempo num
  laço de espera ativa para reduzir a própria velocidade.
- Ruim, porque o motor aloca cerca de 12 bytes por nó, ou 3,4 MB por jogada no
  Difícil, o que gerou cerca de 100 coletas de lixo por partida medida. No M5
  isso não apareceu no frame time; no Quest ainda precisa ser medido.
- Ruim, porque a primeira profundidade da busca não pode ser interrompida, e
  a duração dela não é limitada pelo tempo da jogada num aparelho lento.
- Ruim, porque o Elo é a escala do autor, calibrada contra outros motores, não
  contra pessoas. O Difícil a 2000 pede cerca de 395 mil nós por segundo; o
  IL2CPP mediu 1,56 milhão no M5, e o Quest ainda não foi medido.
