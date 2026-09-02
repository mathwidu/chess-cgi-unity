---
status: accepted
date: 2026-08-29
---

# Usar OpenXR e o XR Interaction Toolkit para o modo VR

## Context and Problem Statement

O [modo VR](../glossary.md) precisa de um framework de XR: algo que aplique a
pose dos [óculos VR](../glossary.md) na câmera, leia a entrada do
[controle de movimento](../glossary.md) e transforme um
[raio de seleção](../glossary.md) apontado em um comando de seleção. Os dois
headsets no roteiro — HTC Vive (conectado a um PC) e Meta Quest 3 (standalone,
além de PC via Link) — são alcançados por SDKs de fabricantes diferentes, mas
o jogo não deve se dividir em dois caminhos de entrada para suportá-los.

## Decision Drivers

- O HTC Vive, o primeiro alvo, não tem SDK tudo-em-um de fabricante — só é
  alcançável por meio do OpenXR (como um runtime OpenXR do SteamVR).
- Um único caminho de código deve atender os dois headsets alvo, em vez de um
  por fabricante.
- O projeto já está na pilha que o ferramental de XR da Unity espera: Unity
  6.3 (6000.3), URP 17.3 e o Input System (1.19), sobre o qual o XR
  Interaction Toolkit é construído.

## Considered Options

- OpenXR Plugin + XR Interaction Toolkit (XRI) da Unity.
- SDK tudo-em-um da Meta (Meta XR / Oculus Integration).

## Decision Outcome

Opção escolhida: "OpenXR Plugin + XR Interaction Toolkit da Unity". O SDK
tudo-em-um da Meta é exclusivo para Quest e não consegue alcançar o Vive de
forma alguma, o que a fase do Vive exige independentemente do que for
escolhido para o Quest depois. O OpenXR é o único caminho que os dois headsets
compartilham: o Vive via SteamVR, o Quest 3 tanto standalone quanto via Link,
cada um selecionado em tempo de execução a partir da lista **Enabled
Interaction Profiles** do OpenXR. O XRI fica sobre o OpenXR e lê a entrada por
meio de ações do Input System, que o projeto já usa.

### Consequences

- Bom, porque a fase do Vive e uma futura fase do Quest se constroem sobre o
  mesmo código de entrada e de rig — sem ramificação por fabricante.
- Bom, porque o OpenXR é a única forma de alcançar o Vive, então isso também
  resolve a escolha de framework para o Quest sem uma decisão separada depois.
- Ruim, porque o suporte do OpenXR ao Quest (via o pacote
  `com.unity.xr.meta-openxr`) precisa de sua própria passagem de configurações
  de projeto quando essa fase começar; este registro não a cobre.
