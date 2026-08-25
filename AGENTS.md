<!-- domainbook:start -->

## Documentation lives in this repo

The book under `domainbook/` documents this codebase, and a commit hook checks it. The rule: changing code a domain claims means updating that domain's book in the same commit, or waiving it with a "Skip-Docs: <reason>" trailer.

| Code | Book |
| --- | --- |
| `game/Assets/Scripts/Rules/**`, `game/Assets/Scripts/Domain/**`, `game/Assets/Scripts/Controllers/ChessGameController.cs` | `domainbook/domains/gameplay/` |
| `game/Assets/Scripts/Controllers/InputController.cs`, `game/Assets/Scripts/Controllers/CameraController.cs` | `domainbook/domains/interaction/` |
| `game/Assets/Scripts/View/**`, `game/Assets/Scripts/UI/**` | `domainbook/domains/presentation/` |

Any file under a domain's folder clears the check for that domain — the canvas, the glossary, the changelog, a feature, a decision, or a debt record. A change across several domains updates each of their books, or carries one record at the book root: a decision under `domainbook/decisions/` or an entry in `domainbook/changelog.md`.

Before you name anything, look the word up and use the one this book already has. The book answers over MCP: call `explain_terms` with the words you are about to use, and `where_to_document` with the paths you are changing. `domainbook serve mcp` starts the server if your client is not connected to it.

Without MCP, the words are written down here:

- `domainbook/domains/gameplay/glossary.md`
- `domainbook/domains/interaction/glossary.md`
- `domainbook/domains/presentation/glossary.md`

When a change means the book has to change too, these procedures say how — reach for the one that fits, and follow its steps there rather than from here:

- `migrate-a-repo` — start a book in a repo that has none.
- `document-this-change` — write the book change a commit or a Stop block is asking for.
- `record-a-decision` — decide whether a choice earns a decision record, and write it if it does.
- `groom-the-glossary` — bring the glossary back in line with the words the code uses.

To waive a commit, end the commit message with a trailer saying what makes the change safe to leave undocumented:

```
Skip-Docs: renamed a private helper, no behaviour or vocabulary changed
```

<!-- domainbook:end -->
