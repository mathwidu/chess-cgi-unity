# Learning Log - Unity

## Marco 1: Projeto e Editor

- Scene: janela onde a cena 3D e editada.
- Game: janela que mostra a camera do jogo.
- Hierarchy: lista de GameObjects da cena.
- Inspector: painel de componentes do GameObject selecionado.
- Project: arquivos do projeto dentro de `Assets`.
- Transform: componente que controla posicao, rotacao e escala.

## Marco 2: Bibliotecas externas

- `Assets/Plugins` permite colocar DLLs que scripts C# da Unity podem referenciar.
- A biblioteca `ChessDotNet` guarda a regra do xadrez fora da apresentacao 3D.
- Separar regra e visual reduz retrabalho quando a cena, modelos ou WebGL mudarem.
- Arquivos `.asmdef` definem assemblies. O Test Runner so encontra testes quando eles estao numa assembly que referencia NUnit e os runners da Unity.
