---
status: accepted
date: 2026-10-19
---

# Usar processo UCI local no candidato Android

## Context and Problem Statement

O usuário autorizou avançar a IA offline do Quest 3 enquanto o modo de
desenvolvedor não está liberado. O transporte desktop usa criação de processos
não implementada pelo IL2CPP instalado; copiar o binário macOS não resolve.

## Considered Options

- Criar processo com `System.Diagnostics.Process`, indisponível no IL2CPP.
- Ligar uma biblioteca do motor ao Unity, exigindo ABI, callbacks e avaliação
  adicional da composição e licença do programa.
- Iniciar o executável empacotado por Java e preservar a comunicação UCI.

## Decision Outcome

Empacotar Stockfish 18 como executável PIE ARM64 em `nativeLibraryDir` e usar
Java `ProcessBuilder` com UCI atrás de `IMoveChooser`. A criação, leitura,
escrita e limpeza não bloqueiam a thread Unity. As filas são limitadas e o
controle de revisão/legalidade permanece no código existente.

O nome `libstockfish.so` é apenas a forma de empacotar/extrair; não há ligação
do motor ao processo Unity, `dlopen`, download em runtime nem executável em
diretório gravável. Licença, autoria, receita e fonte correspondente acompanham
o candidato. O desktop mantém o transporte anterior.

### Consequences

- O desenvolvimento local avança sem uma sessão física, conforme autorizado.
- A rota é sustentada por Android/AOSP; o comportamento do Horizon OS efetivo
  permanece uma hipótese a verificar dentro do aplicativo instalado.
- Falha de processo, timeout e cancelamento continuam recuperáveis. Uma falha
  não autoriza movimentar uma peça sem validação pelas regras.
- O custo de inicialização, memória e CPU concorrendo com VR precisa de medição
  no Quest; os perfis de dificuldade não são uma promessa de Elo ou conforto.

Evidências e fontes estão no [guia da IA Android](../../../../docs/quest3-offline-ai.md).
