# Xadrez 3D CGI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a complete local two-player 3D chess game in Unity for the CGI assignment, with full chess rules, 3D board/pieces, interaction, animation, camera controls, README, report, and video-ready polish.

**Architecture:** Keep chess rules independent from Unity presentation through `ChessRulesAdapter`, then synchronize the 3D scene from the logical board state. Visual behavior is split into board, pieces, input, camera, HUD, and game orchestration scripts so later WebGL, AI, and custom piece models can be added without rewriting the rule layer.

**Tech Stack:** Unity 6.3 LTS `6000.3.16f1`, C#, ChessDotNet `1.0.0` imported as a DLL, Unity EditMode tests, Git, VS Code.

---

## Project Structure

Unity project folder:

- `game/`: Unity project root created from the Unity Hub 3D template.

Repository files outside Unity:

- `README.md`: project overview, how to open/run, controls, delivery notes.
- `.gitignore`: Unity-safe ignore rules.
- `docs/design/chess-cgi-design.md`: approved design spec.
- `docs/report/`: report source and final PDF.
- `docs/video/`: video script/checklist.
- `docs/learning-log.md`: short notes explaining Unity concepts learned per milestone.

Unity files to create:

- `game/Assets/Plugins/ChessDotNet.dll`: chess rules library.
- `game/Assets/Scenes/Main.unity`: main scene.
- `game/Assets/Scripts/Domain/BoardSquare.cs`: board coordinate value type.
- `game/Assets/Scripts/Domain/ChessSide.cs`: local enum for piece color.
- `game/Assets/Scripts/Domain/ChessPieceKind.cs`: local enum for piece kind.
- `game/Assets/Scripts/Domain/VisualPieceState.cs`: piece state used by the visual layer.
- `game/Assets/Scripts/Rules/ChessRulesAdapter.cs`: wrapper around ChessDotNet.
- `game/Assets/Scripts/Rules/MoveResult.cs`: result object returned after attempted moves.
- `game/Assets/Scripts/View/BoardView.cs`: creates board squares and highlights.
- `game/Assets/Scripts/View/SquareView.cs`: clickable square metadata.
- `game/Assets/Scripts/View/PieceView.cs`: clickable piece metadata and animations.
- `game/Assets/Scripts/View/PieceFactory.cs`: creates classic pieces from primitives.
- `game/Assets/Scripts/Controllers/ChessGameController.cs`: game flow orchestration.
- `game/Assets/Scripts/Controllers/InputController.cs`: raycast input and keyboard shortcuts.
- `game/Assets/Scripts/Controllers/CameraController.cs`: orbit, zoom, reset.
- `game/Assets/Scripts/UI/GameHud.cs`: text messages and promotion chooser.
- `game/Assets/Tests/EditMode/BoardSquareTests.cs`: coordinate tests.
- `game/Assets/Tests/EditMode/ChessRulesAdapterTests.cs`: rules smoke tests.

## Task 1: Repository And Unity Project Shell

**Files:**
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/.gitignore`
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/README.md`
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/docs/learning-log.md`
- Create via Unity Hub: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game`

- [ ] **Step 1: Create the Unity project manually**

Open Unity Hub and create a new project:

```text
Template: 3D or 3D (Built-In Render Pipeline)
Project name: game
Location: /Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity
Unity version: 6000.3.16f1
```

Expected result: Unity creates `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game` with `Assets`, `Packages`, and `ProjectSettings`.

- [ ] **Step 2: Create Unity gitignore**

Create `.gitignore` with this content:

```gitignore
# Unity generated folders
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/

# Unity cache and generated files
*.csproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
sysinfo.txt

# macOS
.DS_Store

# VS Code
.vscode/

# Test/build outputs
TestResults/
```

- [ ] **Step 3: Create README skeleton**

Create `README.md` with this content:

```markdown
# Xadrez 3D CGI

Projeto de Computacao Grafica I desenvolvido em Unity 6.3 LTS.

## Como abrir

1. Instale Unity Hub.
2. Instale Unity 6.3 LTS `6000.3.16f1`.
3. No Unity Hub, clique em `Add` e selecione a pasta `game`.
4. Abra a cena `Assets/Scenes/Main.unity`.
5. Clique em `Play`.

## Controles

- Mouse: selecionar peca e casa de destino.
- `Q` / `E`: girar camera.
- Scroll: zoom.
- `R`: resetar camera.
- `N`: nova partida.
- `Esc`: cancelar selecao.

## Entregaveis

- Codigo-fonte Unity em `game/`.
- Relatorio em `docs/report/`.
- Video demonstrativo em `docs/video/`.
```

- [ ] **Step 4: Create learning log**

Create `docs/learning-log.md` with this content:

```markdown
# Learning Log - Unity

## Marco 1: Projeto e Editor

- Scene: janela onde a cena 3D e editada.
- Game: janela que mostra a camera do jogo.
- Hierarchy: lista de GameObjects da cena.
- Inspector: painel de componentes do GameObject selecionado.
- Project: arquivos do projeto dentro de `Assets`.
- Transform: componente que controla posicao, rotacao e escala.
```

- [ ] **Step 5: Commit**

Run:

```bash
git add .gitignore README.md docs/learning-log.md game/Assets game/Packages game/ProjectSettings
git commit -m "chore: create unity project shell"
```

Expected: commit succeeds. If Unity has not generated all folders yet, commit the files/folders that exist and include the same message.

## Task 2: Import Chess Rules Library

**Files:**
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Plugins/ChessDotNet.dll`
- Modify: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/docs/learning-log.md`

- [ ] **Step 1: Download ChessDotNet into Unity**

Run from repo root:

```bash
mkdir -p game/Assets/Plugins
tmp="$(mktemp -d)"
curl -L -o "$tmp/chessdotnet.nupkg" https://api.nuget.org/v3-flatcontainer/chessdotnet/1.0.0/chessdotnet.1.0.0.nupkg
unzip -p "$tmp/chessdotnet.nupkg" lib/netstandard1.3/ChessDotNet.dll > game/Assets/Plugins/ChessDotNet.dll
```

Expected: `game/Assets/Plugins/ChessDotNet.dll` exists.

- [ ] **Step 2: Open Unity and wait for import**

Open the `game` project in Unity. Wait until the spinner/import finishes.

Expected: Unity creates a `.meta` file next to the DLL.

- [ ] **Step 3: Add learning note**

Append this to `docs/learning-log.md`:

```markdown

## Marco 2: Bibliotecas externas

- `Assets/Plugins` permite colocar DLLs que scripts C# da Unity podem referenciar.
- A biblioteca `ChessDotNet` guarda a regra do xadrez fora da apresentacao 3D.
- Separar regra e visual reduz retrabalho quando a cena, modelos ou WebGL mudarem.
```

- [ ] **Step 4: Commit**

Run:

```bash
git add game/Assets/Plugins docs/learning-log.md
git commit -m "chore: import chess rules library"
```

Expected: commit includes the DLL, `.meta` file if generated, and learning log update.

## Task 3: Board Coordinate Domain

**Files:**
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Scripts/Domain/BoardSquare.cs`
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Scripts/Domain/ChessSide.cs`
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Scripts/Domain/ChessPieceKind.cs`
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Scripts/Domain/VisualPieceState.cs`
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Tests/EditMode/BoardSquareTests.cs`

- [ ] **Step 1: Write BoardSquare tests**

Create `game/Assets/Tests/EditMode/BoardSquareTests.cs`:

```csharp
using NUnit.Framework;

public class BoardSquareTests
{
    [Test]
    public void FromAlgebraic_ParsesFileAndRank()
    {
        BoardSquare square = BoardSquare.FromAlgebraic("e4");

        Assert.AreEqual(4, square.FileIndex);
        Assert.AreEqual(4, square.Rank);
        Assert.AreEqual("e4", square.ToAlgebraic());
    }

    [Test]
    public void FromAlgebraic_RejectsInvalidSquare()
    {
        Assert.Throws<System.ArgumentException>(() => BoardSquare.FromAlgebraic("i9"));
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath game \
  -runTests \
  -testPlatform EditMode \
  -testResults TestResults/EditMode.xml \
  -quit
```

Expected: FAIL because `BoardSquare` does not exist.

- [ ] **Step 3: Create domain types**

Create `game/Assets/Scripts/Domain/BoardSquare.cs`:

```csharp
using System;

public readonly struct BoardSquare : IEquatable<BoardSquare>
{
    public int FileIndex { get; }
    public int Rank { get; }

    public BoardSquare(int fileIndex, int rank)
    {
        if (fileIndex < 0 || fileIndex > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(fileIndex), "File index must be between 0 and 7.");
        }

        if (rank < 1 || rank > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be between 1 and 8.");
        }

        FileIndex = fileIndex;
        Rank = rank;
    }

    public static BoardSquare FromAlgebraic(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 2)
        {
            throw new ArgumentException("Square must use algebraic notation like e4.", nameof(value));
        }

        char file = char.ToLowerInvariant(value[0]);
        char rankChar = value[1];

        if (file < 'a' || file > 'h' || rankChar < '1' || rankChar > '8')
        {
            throw new ArgumentException("Square must be between a1 and h8.", nameof(value));
        }

        return new BoardSquare(file - 'a', rankChar - '0');
    }

    public string ToAlgebraic()
    {
        char file = (char)('a' + FileIndex);
        return $"{file}{Rank}";
    }

    public bool Equals(BoardSquare other)
    {
        return FileIndex == other.FileIndex && Rank == other.Rank;
    }

    public override bool Equals(object obj)
    {
        return obj is BoardSquare other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileIndex, Rank);
    }

    public override string ToString()
    {
        return ToAlgebraic();
    }
}
```

Create `game/Assets/Scripts/Domain/ChessSide.cs`:

```csharp
public enum ChessSide
{
    White,
    Black
}
```

Create `game/Assets/Scripts/Domain/ChessPieceKind.cs`:

```csharp
public enum ChessPieceKind
{
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}
```

Create `game/Assets/Scripts/Domain/VisualPieceState.cs`:

```csharp
public readonly struct VisualPieceState
{
    public BoardSquare Square { get; }
    public ChessSide Side { get; }
    public ChessPieceKind Kind { get; }

    public VisualPieceState(BoardSquare square, ChessSide side, ChessPieceKind kind)
    {
        Square = square;
        Side = side;
        Kind = kind;
    }
}
```

- [ ] **Step 4: Run tests to verify pass**

Run the same Unity EditMode command.

Expected: PASS for `BoardSquareTests`.

- [ ] **Step 5: Commit**

Run:

```bash
git add game/Assets/Scripts/Domain game/Assets/Tests/EditMode
git commit -m "feat: add chess board domain types"
```

Expected: commit succeeds.

## Task 4: ChessRulesAdapter

**Files:**
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Scripts/Rules/MoveResult.cs`
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Scripts/Rules/ChessRulesAdapter.cs`
- Create: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/game/Assets/Tests/EditMode/ChessRulesAdapterTests.cs`

- [ ] **Step 1: Write rules adapter tests**

Create `game/Assets/Tests/EditMode/ChessRulesAdapterTests.cs`:

```csharp
using System.Linq;
using NUnit.Framework;

public class ChessRulesAdapterTests
{
    [Test]
    public void NewGame_StartsWithWhiteToMoveAndThirtyTwoPieces()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();

        Assert.AreEqual(ChessSide.White, rules.CurrentTurn);
        Assert.AreEqual(32, rules.GetPieces().Count);
    }

    [Test]
    public void LegalMoves_ForWhitePawnAtE2_IncludeE3AndE4()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();

        string[] moves = rules.GetLegalDestinations(BoardSquare.FromAlgebraic("e2"))
            .Select(square => square.ToAlgebraic())
            .ToArray();

        CollectionAssert.Contains(moves, "e3");
        CollectionAssert.Contains(moves, "e4");
    }

    [Test]
    public void TryMove_E2ToE4_AlternatesTurn()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();

        MoveResult result = rules.TryMove(BoardSquare.FromAlgebraic("e2"), BoardSquare.FromAlgebraic("e4"), null);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ChessSide.Black, rules.CurrentTurn);
        Assert.AreEqual("e4", result.To.ToAlgebraic());
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run the Unity EditMode command.

Expected: FAIL because `ChessRulesAdapter` and `MoveResult` do not exist.

- [ ] **Step 3: Implement MoveResult**

Create `game/Assets/Scripts/Rules/MoveResult.cs`:

```csharp
public readonly struct MoveResult
{
    public bool Success { get; }
    public BoardSquare From { get; }
    public BoardSquare To { get; }
    public bool IsCapture { get; }
    public bool IsCheck { get; }
    public bool IsCheckmate { get; }
    public bool IsDraw { get; }
    public string Message { get; }

    public MoveResult(
        bool success,
        BoardSquare from,
        BoardSquare to,
        bool isCapture,
        bool isCheck,
        bool isCheckmate,
        bool isDraw,
        string message)
    {
        Success = success;
        From = from;
        To = to;
        IsCapture = isCapture;
        IsCheck = isCheck;
        IsCheckmate = isCheckmate;
        IsDraw = isDraw;
        Message = message;
    }

    public static MoveResult Failed(BoardSquare from, BoardSquare to, string message)
    {
        return new MoveResult(false, from, to, false, false, false, false, message);
    }
}
```

- [ ] **Step 4: Implement ChessRulesAdapter**

Create `game/Assets/Scripts/Rules/ChessRulesAdapter.cs`:

```csharp
using System.Collections.Generic;
using ChessDotNet;
using ChessDotNet.Pieces;

public sealed class ChessRulesAdapter
{
    private ChessGame game = new ChessGame();

    public ChessSide CurrentTurn => ToSide(game.WhoseTurn);

    public void Reset()
    {
        game = new ChessGame();
    }

    public List<VisualPieceState> GetPieces()
    {
        List<VisualPieceState> pieces = new List<VisualPieceState>();

        for (int rank = 1; rank <= 8; rank++)
        {
            for (int fileIndex = 0; fileIndex < 8; fileIndex++)
            {
                BoardSquare square = new BoardSquare(fileIndex, rank);
                Piece piece = game.GetPieceAt(ToPosition(square));

                if (piece == null)
                {
                    continue;
                }

                pieces.Add(new VisualPieceState(square, ToSide(piece.Owner), ToKind(piece)));
            }
        }

        return pieces;
    }

    public List<BoardSquare> GetLegalDestinations(BoardSquare from)
    {
        IReadOnlyCollection<Move> moves = game.GetValidMoves(ToPosition(from));
        List<BoardSquare> destinations = new List<BoardSquare>();

        foreach (Move move in moves)
        {
            destinations.Add(FromPosition(move.NewPosition));
        }

        return destinations;
    }

    public MoveResult TryMove(BoardSquare from, BoardSquare to, char? promotion)
    {
        Move move = new Move(ToPosition(from), ToPosition(to), game.WhoseTurn, promotion);

        if (!game.IsValidMove(move))
        {
            return MoveResult.Failed(from, to, "Movimento invalido.");
        }

        Piece capturedPiece;
        MoveType moveType = game.MakeMove(move, true, out capturedPiece);
        Player opponent = ChessUtilities.GetOpponentOf(game.WhoseTurn);
        bool isCheck = game.IsInCheck(game.WhoseTurn);
        bool isCheckmate = game.IsCheckmated(game.WhoseTurn);
        bool isDraw = game.IsDraw() || game.IsStalemated(game.WhoseTurn);
        string message = BuildMessage(moveType, isCheck, isCheckmate, isDraw);

        return new MoveResult(true, from, to, capturedPiece != null, isCheck, isCheckmate, isDraw, message);
    }

    public VisualPieceState? GetPieceAt(BoardSquare square)
    {
        Piece piece = game.GetPieceAt(ToPosition(square));
        if (piece == null)
        {
            return null;
        }

        return new VisualPieceState(square, ToSide(piece.Owner), ToKind(piece));
    }

    private static string BuildMessage(MoveType moveType, bool isCheck, bool isCheckmate, bool isDraw)
    {
        if (isCheckmate)
        {
            return "Xeque-mate.";
        }

        if (isDraw)
        {
            return "Empate.";
        }

        if (isCheck)
        {
            return "Xeque.";
        }

        return moveType.ToString();
    }

    private static Position ToPosition(BoardSquare square)
    {
        return new Position((File)square.FileIndex, square.Rank);
    }

    private static BoardSquare FromPosition(Position position)
    {
        return new BoardSquare((int)position.File, position.Rank);
    }

    private static ChessSide ToSide(Player player)
    {
        return player == Player.White ? ChessSide.White : ChessSide.Black;
    }

    private static ChessPieceKind ToKind(Piece piece)
    {
        if (piece is Pawn)
        {
            return ChessPieceKind.Pawn;
        }

        if (piece is Rook)
        {
            return ChessPieceKind.Rook;
        }

        if (piece is Knight)
        {
            return ChessPieceKind.Knight;
        }

        if (piece is Bishop)
        {
            return ChessPieceKind.Bishop;
        }

        if (piece is Queen)
        {
            return ChessPieceKind.Queen;
        }

        return ChessPieceKind.King;
    }
}
```

- [ ] **Step 5: Run tests to verify pass**

Run the Unity EditMode command.

Expected: PASS for `ChessRulesAdapterTests` and `BoardSquareTests`. If the `File` enum mapping is reversed in ChessDotNet, adjust `ToPosition` and `FromPosition` until `e2` returns `e3` and `e4`.

- [ ] **Step 6: Commit**

Run:

```bash
git add game/Assets/Scripts/Rules game/Assets/Tests/EditMode
git commit -m "feat: wrap chess rules engine"
```

Expected: commit succeeds.

## Task 5: Board And Piece Views

**Files:**
- Create: `game/Assets/Scripts/View/SquareView.cs`
- Create: `game/Assets/Scripts/View/PieceView.cs`
- Create: `game/Assets/Scripts/View/PieceFactory.cs`
- Create: `game/Assets/Scripts/View/BoardView.cs`

- [ ] **Step 1: Create SquareView**

Create `game/Assets/Scripts/View/SquareView.cs`:

```csharp
using UnityEngine;

public sealed class SquareView : MonoBehaviour
{
    public BoardSquare Square { get; private set; }

    public void Initialize(BoardSquare square)
    {
        Square = square;
        gameObject.name = $"Square {square.ToAlgebraic()}";
    }
}
```

- [ ] **Step 2: Create PieceView**

Create `game/Assets/Scripts/View/PieceView.cs`:

```csharp
using System.Collections;
using UnityEngine;

public sealed class PieceView : MonoBehaviour
{
    private Vector3 baseScale;

    public BoardSquare Square { get; private set; }
    public ChessSide Side { get; private set; }
    public ChessPieceKind Kind { get; private set; }

    public void Initialize(VisualPieceState state)
    {
        Square = state.Square;
        Side = state.Side;
        Kind = state.Kind;
        baseScale = transform.localScale;
        gameObject.name = $"{Side} {Kind} {Square.ToAlgebraic()}";
    }

    public void SetSquare(BoardSquare square)
    {
        Square = square;
        gameObject.name = $"{Side} {Kind} {Square.ToAlgebraic()}";
    }

    public void SetSelected(bool selected)
    {
        transform.localScale = selected ? baseScale * 1.15f : baseScale;
    }

    public IEnumerator MoveTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        transform.position = target;
    }
}
```

- [ ] **Step 3: Create PieceFactory**

Create `game/Assets/Scripts/View/PieceFactory.cs`:

```csharp
using UnityEngine;

public sealed class PieceFactory : MonoBehaviour
{
    [SerializeField] private Material whiteMaterial;
    [SerializeField] private Material blackMaterial;

    public PieceView CreatePiece(VisualPieceState state, Vector3 position, Transform parent)
    {
        GameObject root = new GameObject($"{state.Side} {state.Kind}");
        root.transform.SetParent(parent);
        root.transform.position = position;

        PieceView pieceView = root.AddComponent<PieceView>();
        AddCollider(root);
        BuildPrimitiveShape(root.transform, state.Kind, state.Side == ChessSide.White ? whiteMaterial : blackMaterial);
        pieceView.Initialize(state);
        return pieceView;
    }

    private static void AddCollider(GameObject root)
    {
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.4f;
        collider.radius = 0.35f;
        collider.center = new Vector3(0f, 0.7f, 0f);
    }

    private static void BuildPrimitiveShape(Transform parent, ChessPieceKind kind, Material material)
    {
        AddCylinder(parent, "Base", new Vector3(0f, 0.08f, 0f), new Vector3(0.7f, 0.16f, 0.7f), material);
        AddCylinder(parent, "Stem", new Vector3(0f, 0.45f, 0f), new Vector3(0.36f, 0.7f, 0.36f), material);

        switch (kind)
        {
            case ChessPieceKind.Pawn:
                AddSphere(parent, "Head", new Vector3(0f, 0.95f, 0f), new Vector3(0.42f, 0.42f, 0.42f), material);
                break;
            case ChessPieceKind.Rook:
                AddCube(parent, "Crown", new Vector3(0f, 1f, 0f), new Vector3(0.58f, 0.22f, 0.58f), material);
                break;
            case ChessPieceKind.Knight:
                AddCube(parent, "HorseHead", new Vector3(0.1f, 1f, 0f), new Vector3(0.45f, 0.55f, 0.32f), material);
                break;
            case ChessPieceKind.Bishop:
                AddSphere(parent, "Mitre", new Vector3(0f, 1f, 0f), new Vector3(0.48f, 0.62f, 0.48f), material);
                break;
            case ChessPieceKind.Queen:
                AddSphere(parent, "Crown", new Vector3(0f, 1.02f, 0f), new Vector3(0.58f, 0.42f, 0.58f), material);
                AddSphere(parent, "Top", new Vector3(0f, 1.38f, 0f), new Vector3(0.22f, 0.22f, 0.22f), material);
                break;
            case ChessPieceKind.King:
                AddSphere(parent, "Crown", new Vector3(0f, 1.02f, 0f), new Vector3(0.52f, 0.42f, 0.52f), material);
                AddCube(parent, "CrossVertical", new Vector3(0f, 1.42f, 0f), new Vector3(0.12f, 0.4f, 0.12f), material);
                AddCube(parent, "CrossHorizontal", new Vector3(0f, 1.48f, 0f), new Vector3(0.36f, 0.1f, 0.1f), material);
                break;
        }
    }

    private static void AddCylinder(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ConfigurePart(part, parent, name, localPosition, localScale, material);
    }

    private static void AddSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ConfigurePart(part, parent, name, localPosition, localScale, material);
    }

    private static void AddCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ConfigurePart(part, parent, name, localPosition, localScale, material);
    }

    private static void ConfigurePart(GameObject part, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        Object.Destroy(part.GetComponent<Collider>());
        part.name = name;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
}
```

- [ ] **Step 4: Create BoardView**

Create `game/Assets/Scripts/View/BoardView.cs` with methods to build squares, map board squares to world positions, highlight legal moves, and sync piece views from `VisualPieceState` values.

Required behavior:

```text
Square size: 1.25 Unity units
Board origin: centered around world zero
White perspective: a1 at bottom-left from the default camera
Square height: y = 0
Piece base height: y = 0.08
```

Expected: after wiring in Task 6, the scene shows a centered 8x8 board with pieces on correct squares.

- [ ] **Step 5: Commit**

Run:

```bash
git add game/Assets/Scripts/View
git commit -m "feat: add board and piece views"
```

Expected: commit succeeds.

## Task 6: Game Controller, Input, HUD, And Camera

**Files:**
- Create: `game/Assets/Scripts/Controllers/ChessGameController.cs`
- Create: `game/Assets/Scripts/Controllers/InputController.cs`
- Create: `game/Assets/Scripts/Controllers/CameraController.cs`
- Create: `game/Assets/Scripts/UI/GameHud.cs`
- Modify scene: `game/Assets/Scenes/Main.unity`

- [ ] **Step 1: Implement controller responsibilities**

Create scripts with these responsibilities:

```text
ChessGameController:
- Owns ChessRulesAdapter.
- Tracks selected PieceView.
- Asks rules for legal destinations.
- Calls BoardView to show/clear highlights.
- Starts PieceView movement coroutine.
- Re-syncs all pieces after the move.
- Shows HUD messages.
- Blocks input while animation is running.

InputController:
- On left click, raycast from Camera.main.
- If hit PieceView, call ChessGameController.SelectPiece.
- If hit SquareView, call ChessGameController.SelectSquare.
- Escape cancels selection.
- N restarts the match.

CameraController:
- Q/E orbit around board center.
- Mouse scroll changes camera distance.
- R restores default camera transform.

GameHud:
- Shows current turn and status text.
- Shows promotion choice buttons for Q/R/B/N.
```

- [ ] **Step 2: Create scene objects manually**

In Unity:

```text
1. Create empty GameObject: GameManager.
2. Add ChessGameController.
3. Add InputController.
4. Create empty GameObject: Board.
5. Create empty GameObject: Pieces.
6. Create empty GameObject: Highlights.
7. Add BoardView to Board.
8. Add PieceFactory to Pieces.
9. Create Canvas and add GameHud.
10. Add CameraController to Main Camera.
11. Assign serialized references in Inspector.
```

Expected: scene hierarchy has `GameManager`, `Board`, `Pieces`, `Highlights`, `Main Camera`, `Directional Light`, and `Canvas`.

- [ ] **Step 3: Create materials manually**

In Unity Project window:

```text
Assets/Materials/BoardLight.mat
Assets/Materials/BoardDark.mat
Assets/Materials/PieceWhite.mat
Assets/Materials/PieceBlack.mat
Assets/Materials/MoveHighlight.mat
Assets/Materials/SelectedHighlight.mat
Assets/Materials/CheckHighlight.mat
```

Use colors:

```text
BoardLight: warm light gray
BoardDark: muted dark teal
PieceWhite: ivory
PieceBlack: charcoal
MoveHighlight: transparent green
SelectedHighlight: gold
CheckHighlight: red
```

Expected: materials are assignable to `BoardView` and `PieceFactory`.

- [ ] **Step 4: Play test in Unity**

Click Play.

Expected:

```text
- Board appears.
- Pieces appear in initial position.
- Clicking a white piece highlights legal squares.
- Clicking legal square moves the piece.
- Turn changes to black.
- Camera responds to Q/E/scroll/R.
```

- [ ] **Step 5: Commit**

Run:

```bash
git add game/Assets/Scripts/Controllers game/Assets/Scripts/UI game/Assets/Scenes game/Assets/Materials
git commit -m "feat: make chess scene playable"
```

Expected: commit succeeds.

## Task 7: Full Rules Visual Verification

**Files:**
- Modify: `game/Assets/Scripts/Rules/ChessRulesAdapter.cs`
- Modify: `game/Assets/Scripts/Controllers/ChessGameController.cs`
- Modify: `game/Assets/Scripts/UI/GameHud.cs`
- Modify: `docs/learning-log.md`

- [ ] **Step 1: Verify special moves manually**

Use controlled games to verify:

```text
Promotion: move a pawn to last rank and choose Q/R/B/N.
Castling: clear path and castle kingside/queenside.
En passant: create legal en passant position and capture.
Check: move queen/bishop/rook into attack line.
Checkmate: perform a short mate pattern such as Fool's Mate.
Stalemate/draw: verify library reports draw when reachable.
```

Expected: visual board matches the rules engine after every move.

- [ ] **Step 2: Add HUD messages**

HUD must display:

```text
Turno: Brancas
Turno: Pretas
Xeque
Xeque-mate - Brancas vencem
Xeque-mate - Pretas vencem
Empate
Escolha a promocao
Movimento invalido
```

- [ ] **Step 3: Add learning note**

Append to `docs/learning-log.md`:

```markdown

## Marco 3: Regra e visual

- O estado logico do jogo vem do motor de regras.
- A cena Unity e uma representacao visual desse estado.
- Re-sincronizar a cena apos cada jogada simplifica casos especiais como roque, promocao e en passant.
- A animacao usa interpolacao entre posicoes 3D ao longo do tempo.
```

- [ ] **Step 4: Commit**

Run:

```bash
git add game/Assets/Scripts docs/learning-log.md
git commit -m "feat: verify full chess rule flow"
```

Expected: commit succeeds.

## Task 8: CGI Polish And Delivery Assets

**Files:**
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Modify: `game/Assets/Scripts/Controllers/CameraController.cs`
- Modify: `game/Assets/Scenes/Main.unity`
- Create: `docs/report/relatorio-xadrez-cgi.md`
- Create: `docs/video/demo-script.md`
- Modify: `README.md`

- [ ] **Step 1: Add visible CGI polish**

Add:

```text
- Smooth piece movement arc or lift during movement.
- Scale feedback for selected piece.
- Transparent highlights on legal squares.
- Red highlight on checked king.
- Camera angle that shows board depth and all pieces.
- Directional Light with shadows enabled.
```

- [ ] **Step 2: Write report draft**

Create `docs/report/relatorio-xadrez-cgi.md` with sections:

```markdown
# Xadrez 3D em Unity

## Descricao do projeto

## Tecnologias utilizadas

## Fundamentacao tecnica

## Transformacoes geometricas

## Animacao e tempo

## Modelagem 3D

## Interatividade

## Desafios e solucoes

## Conclusao e melhorias futuras
```

- [ ] **Step 3: Write video script**

Create `docs/video/demo-script.md`:

```markdown
# Video demonstrativo - roteiro

1. Mostrar cena inicial e explicar tabuleiro/pecas.
2. Selecionar uma peca e mostrar casas legais.
3. Executar movimento com animacao.
4. Mostrar captura.
5. Mostrar roque ou promocao.
6. Mostrar controle de camera.
7. Mostrar xeque ou xeque-mate.
8. Encerrar mostrando README e estrutura do projeto.
```

- [ ] **Step 4: Update README**

README must include:

```text
- Unity version.
- How to open `game/`.
- How to play.
- Controls.
- Folder structure.
- Known optional extras: WebGL, IA, custom pieces.
```

- [ ] **Step 5: Commit**

Run:

```bash
git add README.md docs/report docs/video game/Assets
git commit -m "docs: prepare assignment delivery materials"
```

Expected: commit succeeds.

## Task 9: Final Verification

**Files:**
- Modify only files needed to fix final verification issues.

- [ ] **Step 1: Run Unity EditMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath game \
  -runTests \
  -testPlatform EditMode \
  -testResults TestResults/EditMode.xml \
  -quit
```

Expected: tests pass.

- [ ] **Step 2: Manual playthrough**

In Unity Play mode verify:

```text
- Initial board is correct.
- White starts.
- Illegal moves are blocked.
- Legal moves animate.
- Captures work.
- Turn alternates.
- Promotion works.
- Castling works.
- En passant works.
- Check/checkmate/stalemate messages are shown.
- Camera controls work.
- New game works.
```

- [ ] **Step 3: Delivery readiness**

Verify:

```text
- README is current.
- Report has 2 to 4 pages after PDF export.
- Video is at most 3 minutes.
- Git status is clean.
```

- [ ] **Step 4: Commit final fixes**

Run only if there are final changes:

```bash
git add README.md docs game/Assets game/Packages game/ProjectSettings
git commit -m "chore: finalize cgi chess delivery"
```

Expected: git status is clean after commit.

## Self-Review

- Spec coverage: the plan covers Unity setup, 3D board, pieces, full rules, interaction, animation, camera, README, report, video, and verification.
- Deferred-marker scan: no unresolved marker text or unspecified implementation-only steps remain.
- Type consistency: domain names are stable across tasks: `BoardSquare`, `ChessSide`, `ChessPieceKind`, `VisualPieceState`, `ChessRulesAdapter`, `MoveResult`, `BoardView`, `PieceView`, `PieceFactory`, `ChessGameController`, `InputController`, `CameraController`, and `GameHud`.
- Risk note: `BoardView`, controllers, and HUD are intentionally implemented after Unity project creation because scene wiring needs Unity-generated files and Inspector references.
