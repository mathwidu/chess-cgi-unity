# Windows Handoff Repository Setup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prepare the Unity chess project to move between Mac and Windows with a safe stable branch, an active improvement branch, documented setup, and repeatable validation.

**Architecture:** Keep the Unity project under `game/` and keep generated/cache folders out of git. Preserve the stable release as a branch/tag and continue improvements on feature branches. Document Windows setup as a first-class handoff so a fresh Codex session can configure Unity, Blender, MCP, tests, and delivery without relying on chat context.

**Tech Stack:** Git, GitHub, Unity 6.3 LTS `6000.3.16f1`, Unity Test Framework, Codex Desktop, Unity MCP, Blender, Blender MCP, Python, PowerShell.

---

### Task 1: Preserve Stable Git References

**Files:**
- No file changes required.

- [ ] **Step 1: Verify current references**

Run:

```bash
git status --short --branch
git tag --list --sort=-creatordate
git branch --all --verbose --no-abbrev
git rev-list -n 1 entrega-v1-estavel
```

Expected:

```text
entrega-v1-estavel resolves to a9620344e242730d999eb1fce5d2898fa617df46
feature/animated-pieces-and-sidebar exists
```

- [ ] **Step 2: Create stable branch if missing**

Run:

```bash
git branch --list stable/entrega-v1-estavel
git branch stable/entrega-v1-estavel entrega-v1-estavel^{}
```

Expected:

```text
stable/entrega-v1-estavel points to a9620344e242730d999eb1fce5d2898fa617df46
```

- [ ] **Step 3: Do not checkout stable branch for active work**

Run:

```bash
git switch feature/animated-pieces-and-sidebar
```

Expected:

```text
Already on 'feature/animated-pieces-and-sidebar'
```

### Task 2: Add Cross-Platform Git Attributes

**Files:**
- Create: `.gitattributes`

- [ ] **Step 1: Create `.gitattributes`**

Content:

```gitattributes
*.cs text eol=lf
*.asmdef text eol=lf
*.unity text eol=lf merge=unityyamlmerge
*.prefab text eol=lf merge=unityyamlmerge
*.asset text eol=lf merge=unityyamlmerge
*.mat text eol=lf merge=unityyamlmerge
*.meta text eol=lf merge=unityyamlmerge
*.controller text eol=lf merge=unityyamlmerge
*.anim text eol=lf merge=unityyamlmerge
*.json text eol=lf
*.md text eol=lf
*.py text eol=lf
*.ps1 text eol=crlf
*.glb binary
*.fbx binary
*.png binary
*.jpg binary
*.jpeg binary
*.exr binary
*.tga binary
*.wav binary
*.mp3 binary
*.mp4 binary
*.zip binary
```

- [ ] **Step 2: Validate whitespace**

Run:

```bash
git diff --check
```

Expected: no output.

### Task 3: Create Windows Codex Handoff

**Files:**
- Create: `CODEX_WINDOWS_HANDOFF.md`

- [ ] **Step 1: Write handoff document**

Include:

```markdown
# Codex Windows Handoff

Este arquivo e o ponto de partida para continuar o projeto no PC Windows.

- Projeto Unity real: `game/`
- Cena principal: `game/Assets/Scenes/Main.unity`
- Unity exigida: `6000.3.16f1`
- Branch de entrega segura: `stable/entrega-v1-estavel`
- Tag de entrega segura: `entrega-v1-estavel`
- Branch ativa de melhorias: `feature/animated-pieces-and-sidebar`
```

- [ ] **Step 2: Add safety rules**

Include:

```markdown
- Nao editar a branch `stable/entrega-v1-estavel`.
- Trabalhar em `feature/animated-pieces-and-sidebar` ou branch nova.
- Nao versionar `game/Library`, `game/Temp`, `game/UserSettings`, `Builds`, `Logs`, `TestResults` ou fotos privadas.
- Testar Blender MCP em cena descartavel antes de tocar nos assets do jogo.
```

- [ ] **Step 3: Add validation commands**

Include PowerShell:

```powershell
py -3 -m unittest tools.blender.tests.test_all_piece_side_variants -v
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe"
& $Unity -batchmode -quit -projectPath "$PWD\game" -runTests -testPlatform EditMode -testResults "$PWD\TestResults\editmode-windows.xml" -logFile "$PWD\Logs\unity-editmode-windows.log"
```

### Task 4: Document Windows Environment Setup

**Files:**
- Create: `docs/setup/windows-environment-setup.md`

- [ ] **Step 1: Document tool install commands**

Include:

```powershell
winget install --id Git.Git -e
winget install --id GitHub.cli -e
winget install --id Microsoft.VisualStudioCode -e
winget install --id Python.Python.3.13 -e
winget install --id BlenderFoundation.Blender -e
winget install --id Unity.UnityHub -e
```

- [ ] **Step 2: Document Unity version**

Include:

```markdown
Install Unity 6.3 LTS `6000.3.16f1` with Windows Build Support.
Open `chess-cgi-unity/game` from Unity Hub.
Open `Assets/Scenes/Main.unity`.
```

- [ ] **Step 3: Document Unity MCP**

Include:

```markdown
Open `Edit > Project Settings > AI > Unity MCP Server`.
Confirm `Unity Bridge: Running`.
Restart Codex Desktop.
Run a read-only Unity MCP command before edits.
```

- [ ] **Step 4: Document Blender MCP**

Include:

```powershell
mkdir "$HOME\.codex\mcp-vendor" -Force
git clone https://github.com/ahujasid/blender-mcp.git "$HOME\.codex\mcp-vendor\blender-mcp"
cd "$HOME\.codex\mcp-vendor\blender-mcp"
git checkout f76420613e5abb7c965df7ca84a1c52f3a211c5b
```

Expected: the MCP is pinned to the same commit documented on Mac.

### Task 5: Document Repository And Delivery Flow

**Files:**
- Create: `docs/setup/repository-delivery-flow.md`

- [ ] **Step 1: Document branches**

Include:

```markdown
- `stable/entrega-v1-estavel`: frozen safe delivery.
- `feature/animated-pieces-and-sidebar`: active improvement work.
- `main`: base/historical line.
```

- [ ] **Step 2: Document initial remote push**

Include:

```bash
git remote add origin <URL_DO_REPOSITORIO>
git push -u origin main
git push -u origin feature/chess-mvp
git push -u origin feature/animated-pieces-and-sidebar
git push -u origin stable/entrega-v1-estavel
git push origin --tags
```

- [ ] **Step 3: Document final delivery tag**

Include:

```bash
git tag -a entrega-final-2026-06-11 -m "Entrega final do Xadrez CGI"
git push origin entrega-final-2026-06-11
```

### Task 6: Validate Handoff Artifacts

**Files:**
- Validate: `.gitattributes`
- Validate: `CODEX_WINDOWS_HANDOFF.md`
- Validate: `docs/setup/windows-environment-setup.md`
- Validate: `docs/setup/repository-delivery-flow.md`

- [ ] **Step 1: Run repository checks**

Run:

```bash
git diff --check
git status --short --branch
```

Expected:

```text
git diff --check has no output
new docs and .gitattributes appear as untracked or staged files
stable branch remains untouched
```

- [ ] **Step 2: Verify no remote was created accidentally**

Run:

```bash
git remote -v
```

Expected:

```text
No output until the user explicitly creates/provides the remote repository URL.
```

- [ ] **Step 3: Commit after user review**

After the user approves the docs and any current working-tree scope:

```bash
git add .gitattributes CODEX_WINDOWS_HANDOFF.md docs/setup docs/superpowers/plans/2026-06-08-windows-handoff-repository-setup.md
git commit -m "docs: add windows handoff and repository setup"
```

Expected: a commit on `feature/animated-pieces-and-sidebar`.

