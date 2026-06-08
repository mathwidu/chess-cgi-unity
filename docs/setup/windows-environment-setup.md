# Windows Environment Setup

Objetivo: preparar um PC Windows para abrir, testar e continuar o projeto `Xadrez CGI` com Unity, Codex, Unity MCP, Blender e Blender MCP.

## 1. Instalar ferramentas base

Abra PowerShell como usuario normal e confira se `winget` existe:

```powershell
winget --version
```

Instale as ferramentas principais:

```powershell
winget install --id Git.Git -e
winget install --id GitHub.cli -e
winget install --id Microsoft.VisualStudioCode -e
winget install --id Python.Python.3.13 -e
winget install --id BlenderFoundation.Blender -e
winget install --id Unity.UnityHub -e
```

Se algum pacote falhar no `winget`, use o instalador oficial da ferramenta e volte para a validacao abaixo.

Feche e reabra o PowerShell. Valide:

```powershell
git --version
gh --version
code --version
py -3 --version
blender --version
```

## 2. Instalar Unity correta

No Unity Hub:

1. Abra `Installs`.
2. Clique em `Install Editor`.
3. Instale `Unity 6.3 LTS 6000.3.16f1`.
4. Marque pelo menos:
   - Windows Build Support;
   - Visual Studio/VS Code editor integration, se aparecer;
   - Documentation, se houver espaco.
5. Nao precisa instalar Android, iOS, tvOS, visionOS ou Linux para este trabalho agora.

Validar caminho padrao:

```powershell
Test-Path "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
```

Resultado esperado:

```text
True
```

## 3. Clonar o projeto

Depois que o repositorio remoto existir:

```powershell
cd $HOME
mkdir projetos
cd projetos
git clone <URL_DO_REPOSITORIO> chess-cgi-unity
cd chess-cgi-unity
git fetch --all --tags
git switch feature/animated-pieces-and-sidebar
```

Se quiser abrir a versao estavel de entrega:

```powershell
git switch stable/entrega-v1-estavel
```

Para voltar ao trabalho de melhorias:

```powershell
git switch feature/animated-pieces-and-sidebar
```

## 4. Abrir na Unity

No Unity Hub:

1. Clique em `Add`.
2. Selecione a pasta `chess-cgi-unity/game`.
3. Abra o projeto.
4. Abra a cena `Assets/Scenes/Main.unity`.
5. Espere a importacao terminar.
6. Clique em `Play`.

Se a Unity perguntar sobre Input System/restart, aceite e deixe o Editor reiniciar.

## 5. Validar testes no Windows

EditMode via PowerShell:

```powershell
cd $HOME\projetos\chess-cgi-unity
mkdir TestResults -Force
mkdir Logs -Force

$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
& $Unity -batchmode -quit `
  -projectPath "$PWD\game" `
  -runTests `
  -testPlatform EditMode `
  -testResults "$PWD\TestResults\editmode-windows.xml" `
  -logFile "$PWD\Logs\unity-editmode-windows.log"
```

PlayMode via PowerShell:

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
& $Unity -batchmode -quit `
  -projectPath "$PWD\game" `
  -runTests `
  -testPlatform PlayMode `
  -testResults "$PWD\TestResults\playmode-windows.xml" `
  -logFile "$PWD\Logs\unity-playmode-windows.log"
```

Python tests do pipeline Blender:

```powershell
py -3 -m unittest `
  tools.blender.tests.test_character_definition `
  tools.blender.tests.test_character_quality_manifest `
  tools.blender.tests.test_mathwidu_v3b_candidate `
  tools.blender.tests.test_all_piece_side_variants `
  -v
```

## 6. Unity MCP

O pacote `com.unity.ai.assistant` ja esta em `game/Packages/manifest.json`.

No Unity:

1. Abra `Edit > Project Settings`.
2. Entre em `AI > Unity MCP Server`.
3. Confirme `Unity Bridge: Running`.
4. Se nao estiver rodando, clique em `Start`.
5. Reinicie o Codex Desktop no Windows.
6. No Codex, procure ferramentas Unity MCP e rode um comando read-only primeiro.

Teste read-only sugerido pelo Codex:

```csharp
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log("Unity MCP conectado no projeto: {0}", UnityEngine.Application.productName);
    }
}
```

## 7. Blender CLI

Validar se o Blender esta no PATH:

```powershell
blender --version
```

Se nao estiver, use o caminho instalado. Exemplos comuns:

```powershell
$Blender = "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe"
& $Blender --version
```

Validar numpy dentro do Python do Blender:

```powershell
& $Blender --background --python-expr "import numpy; print(numpy.__version__)"
```

Regenerar variantes de lado:

```powershell
& $Blender --background --python tools/blender/create_all_piece_side_variants.py
```

Validar depois:

```powershell
py -3 -m unittest tools.blender.tests.test_all_piece_side_variants -v
```

## 8. Blender MCP seguro

Regra: configurar e testar primeiro em uma cena descartavel.

Instalar `uv`:

```powershell
winget install --id astral-sh.uv -e
uvx --version
```

Baixar o Blender MCP pinado:

```powershell
mkdir "$HOME\.codex\mcp-vendor" -Force
git clone https://github.com/ahujasid/blender-mcp.git "$HOME\.codex\mcp-vendor\blender-mcp"
cd "$HOME\.codex\mcp-vendor\blender-mcp"
git checkout f76420613e5abb7c965df7ca84a1c52f3a211c5b
```

Copiar addon para Blender:

```powershell
$AddonDir = "$env:APPDATA\Blender Foundation\Blender\5.1\scripts\addons"
mkdir $AddonDir -Force
copy "$HOME\.codex\mcp-vendor\blender-mcp\addon.py" "$AddonDir\blender_mcp.py"
```

Se o arquivo `addon.py` nao existir nesse checkout, pare e inspecione o repositorio antes de copiar qualquer outro arquivo.

Configurar Codex em `$HOME\.codex\config.toml`:

```toml
[mcp_servers.blender]
command = "uvx"
args = ["--from", "C:\\Users\\SEU_USUARIO\\.codex\\mcp-vendor\\blender-mcp", "blender-mcp"]
startup_timeout_sec = 120

[mcp_servers.blender.env]
DISABLE_TELEMETRY = "true"
BLENDER_HOST = "localhost"
BLENDER_PORT = "9876"
```

Troque `SEU_USUARIO` pelo nome real da pasta em `C:\Users`.

No Blender:

1. Abra uma cena nova vazia.
2. Ative o addon `Blender MCP`.
3. Confirme que integracoes externas continuam desativadas.
4. Salve preferencias.
5. Reinicie o Codex Desktop.
6. Rode um health check read-only.

Seguranca:

- permitido: listar cena, capturar screenshot, inspecionar objetos e materiais;
- revisar antes: qualquer script que escreva arquivo, exporte asset ou rode Python arbitrario;
- bloquear sem aprovacao: API keys, Sketchfab, Hyper3D, Hunyuan3D, downloads externos e codigo com `os`, `subprocess`, `socket`, `requests`, `urllib` ou `shutil`.

## 9. Build Windows jogavel

Pelo Editor:

1. Abra `Assets/Scenes/Main.unity`.
2. Abra `File > Build Profiles`.
3. Selecione Windows.
4. Garanta que `Assets/Scenes/Main.unity` esta na lista.
5. Build em `Builds/Windows/XadrezCGI.exe`.

Builds sao ignoradas pelo git. Entregue build zipada separadamente, se o professor pedir executavel.

