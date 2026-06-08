# Codex Windows Handoff

Este arquivo e o ponto de partida para continuar o projeto no PC Windows.

## Contexto rapido

- Projeto Unity real: `game/`
- Cena principal: `game/Assets/Scenes/Main.unity`
- Unity exigida: `6000.3.16f1`
- Branch de entrega segura: `stable/entrega-v1-estavel`
- Tag de entrega segura: `entrega-v1-estavel`
- Branch ativa de melhorias: `feature/animated-pieces-and-sidebar`
- O projeto usa Unity MCP e Blender MCP como ferramentas auxiliares, mas a fonte de verdade deve continuar sendo codigo, scripts e docs versionados no repositorio.

## Primeira leitura obrigatoria

1. `README.md`
2. `docs/setup/windows-environment-setup.md`
3. `docs/setup/repository-delivery-flow.md`
4. `docs/design/custom-piece-generation-workflow.md`
5. `docs/design/professional-rigging-animation-roadmap.md`
6. `docs/design/blender-codex-integration-security-plan.md`
7. `docs/design/blender-mcp-local-setup.md`

## Regras para o Codex no Windows

- Nao editar a branch `stable/entrega-v1-estavel`.
- Trabalhar em `feature/animated-pieces-and-sidebar` ou em uma branch nova criada a partir dela.
- Nao versionar `game/Library`, `game/Temp`, `game/UserSettings`, `Builds`, `Logs`, `TestResults` ou fotos privadas.
- Nao instalar skill, MCP server, addon do Blender ou pacote Python sem revisar a origem e confirmar com o usuario.
- Testar Blender MCP em cena descartavel antes de tocar nos assets do jogo.
- Manter regras de xadrez isoladas de polimento visual. O polish deve ficar em camadas como `PieceFactory`, `PieceView`, `PieceMotionController`, `GameHud`, `SelectedPiecePreviewController`, `ScenePolish` e scripts de asset pipeline.
- Antes de afirmar que algo esta pronto, rodar validacoes e relatar o resultado real.

## Setup minimo esperado no Windows

Instalar:

- Git for Windows
- Git LFS, opcional agora, recomendado antes de assets grandes
- Unity Hub
- Unity Editor `6000.3.16f1`
- Visual Studio Code ou Rider
- Blender
- Python 3
- uv ou uvx para MCP Python
- Codex Desktop

Depois:

```powershell
git clone <URL_DO_REPOSITORIO> chess-cgi-unity
cd chess-cgi-unity
git fetch --all --tags
git switch feature/animated-pieces-and-sidebar
```

Abrir no Unity Hub a pasta:

```text
chess-cgi-unity/game
```

## Validacao rapida depois do clone

```powershell
git status --short --branch
py -3 -m unittest tools.blender.tests.test_all_piece_side_variants -v
```

Validacao Unity em batch:

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
& $Unity -batchmode -quit `
  -projectPath "$PWD\game" `
  -runTests `
  -testPlatform EditMode `
  -testResults "$PWD\TestResults\editmode-windows.xml" `
  -logFile "$PWD\Logs\unity-editmode-windows.log"
```

Se a Unity ainda estiver importando pacotes pela primeira vez, abra pelo Hub uma vez antes de rodar a suite em batch.

## Proximo objetivo tecnico

1. Confirmar que o Windows abre a cena `Main.unity`.
2. Confirmar que os prefabs customizados por lado aparecem no tabuleiro.
3. Confirmar Unity MCP rodando.
4. Confirmar Blender MCP em cena descartavel.
5. Continuar a fase de animacoes reais: inventario de rigs, prova vertical com peao e depois aplicacao gradual aos demais personagens.

