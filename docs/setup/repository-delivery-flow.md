# Repository And Delivery Flow

Objetivo: organizar o repositorio para facilitar trabalho entre Mac, Windows e entrega final.

## Estado atual validado

Em 2026-06-08:

```text
branch atual: feature/animated-pieces-and-sidebar
tag estavel: entrega-v1-estavel -> a9620344e242730d999eb1fce5d2898fa617df46
tag polida: entrega-v2-polida -> 2baa7b670585a994e3a94632a2f72d6d68285804
branch estavel local: stable/entrega-v1-estavel -> a9620344e242730d999eb1fce5d2898fa617df46
remote: nenhum configurado ainda
```

## Branches oficiais do trabalho

- `stable/entrega-v1-estavel`: linha congelada para entrega segura. Nao desenvolver nela.
- `feature/animated-pieces-and-sidebar`: linha de melhorias atuais: personagens por lado, UI, movimento, captura e rigging.
- `main`: linha base/historica do projeto. Pode receber merge depois que uma entrega for aprovada.

Tags:

- `entrega-v1-estavel`: primeira entrega jogavel segura.
- `entrega-v2-polida`: entrega polida validada antes das melhorias atuais.

## Antes de publicar no GitHub

No Mac, rodar:

```bash
cd /Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity
git status --short --branch
git diff --check
python3 -m unittest \
  tools.blender.tests.test_character_definition \
  tools.blender.tests.test_character_quality_manifest \
  tools.blender.tests.test_mathwidu_v3b_candidate \
  tools.blender.tests.test_all_piece_side_variants \
  -v
```

Se houver arquivos indesejados em `git status`, nao publique ainda. Conferir especialmente:

- `game/Library/`
- `game/Temp/`
- `game/UserSettings/`
- `Builds/`
- `Logs/`
- `TestResults/`
- fotos pessoais em `game/Assets/Art/PrivateReferences/`

Esses arquivos devem ficar ignorados.

## Criar repositorio remoto

Opção recomendada:

1. Criar um repositorio vazio no GitHub chamado `chess-cgi-unity`.
2. Nao criar README, `.gitignore` ou license pelo GitHub, porque o projeto ja tem esses arquivos localmente.
3. Decidir se sera privado ou publico. Para entrega com fotos/personagens de colegas, privado e mais prudente.

Depois, no Mac:

```bash
git remote add origin <URL_DO_REPOSITORIO>
git remote -v
```

Push inicial:

```bash
git push -u origin main
git push -u origin feature/chess-mvp
git push -u origin feature/animated-pieces-and-sidebar
git push -u origin stable/entrega-v1-estavel
git push origin --tags
```

Se `git push` recusar por arquivo grande, parar e configurar Git LFS antes de tentar de novo.

## Git LFS

Nao e obrigatorio agora porque os GLBs atuais sao pequenos, mas e recomendado antes de assets maiores.

Instalar:

```bash
git lfs install
```

Se decidirmos usar LFS:

```bash
git lfs track "*.glb"
git lfs track "*.fbx"
git lfs track "*.png"
git add .gitattributes
git commit -m "chore: track large art assets with git lfs"
```

Nao converter historico para LFS sem necessidade. Para este projeto curto, so ativar se algum arquivo novo passar de 50 MB ou se o push remoto reclamar.

## Fluxo de trabalho no Windows

```powershell
git clone <URL_DO_REPOSITORIO> chess-cgi-unity
cd chess-cgi-unity
git fetch --all --tags
git switch feature/animated-pieces-and-sidebar
```

Criar branch de trabalho curta:

```powershell
git switch -c feature/windows-rigging-validation
```

Ao terminar uma tarefa:

```powershell
git status --short
git diff --check
py -3 -m unittest tools.blender.tests.test_all_piece_side_variants -v
git add -A
git status --short
git commit -m "feat: validate windows character pipeline"
git push -u origin feature/windows-rigging-validation
```

## Como recuperar a versao segura

No Windows ou Mac:

```bash
git fetch --all --tags
git switch stable/entrega-v1-estavel
```

Para apenas inspecionar a tag:

```bash
git switch --detach entrega-v1-estavel
```

Voltar para melhorias:

```bash
git switch feature/animated-pieces-and-sidebar
```

## Checklist de entrega final

- [ ] `README.md` atualizado com estado real do jogo.
- [ ] `docs/report/` com relatorio final de 2 a 4 paginas.
- [ ] `docs/video/` com roteiro/link do video demonstrativo de ate 3 minutos.
- [ ] Testes EditMode rodam sem erro.
- [ ] PlayMode smoke test passa.
- [ ] `Main.unity` abre no Windows.
- [ ] Build Windows gerada se o professor precisar jogar fora do Editor.
- [ ] Tag final criada, por exemplo `entrega-final-2026-06-11`.
- [ ] Branch final enviada ao GitHub.

Criar tag final:

```bash
git tag -a entrega-final-2026-06-11 -m "Entrega final do Xadrez CGI"
git push origin entrega-final-2026-06-11
```

## Comando para conferir branches importantes

```bash
git branch --all --verbose --no-abbrev
git tag --list --sort=-creatordate
git remote -v
```

