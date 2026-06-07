# Chess Game

A two-player chess game built with C# and SDL2 (via Silk.NET) on the ProjectSkeleton starter repo.

## How to Play

- **Click** a piece to select it (yellow highlight appears)
- **Green dots** show valid moves for the selected piece
- **Click** a destination to move the piece
- **Click** another of your pieces to switch selection
- Press **R** to restart the game
- Press **ESC** to quit

## Features

- Full chess rules: all piece movements, castling, en passant, pawn promotion (auto-queen)
- Check, checkmate, and stalemate detection
- Valid move highlighting can be visible in terminal output (console logs) since no textures/fonts are used
- Game statistics (wins, draws) saved to disk between runs
- Turn indicator and move logging in the console

## Visual Guide (Piece Shapes)

Since the game uses SDL2 primitives (no textures or fonts), pieces are drawn as colored rectangles with inner markings:

| Piece  | White Color | Black Color | Inner Marking        |
|--------|-------------|-------------|----------------------|
| King   | Light blue  | Dark red    | Cross (+)            |
| Queen  | Light blue  | Dark red    | X + horizontal line  |
| Rook   | Light blue  | Dark red    | Inner square         |
| Bishop | Light blue  | Dark red    | Diagonal X           |
| Knight | Light blue  | Dark red    | L-shape              |
| Pawn   | Light blue  | Dark red    | Small dot            |

Piece size also varies: pawns are smallest, king and queen are largest.

## Build & Run

```bash
dotnet run
```

Requires .NET 10 SDK. Builds and runs on Windows (also Linux/macOS with SDL2 available).

## C# Features Demonstrated

- **Interfaces**: `IRenderable` for drawable objects
- **Inheritance/Polymorphism**: `ChessPiece` implements `IRenderable`
- **Pattern matching**: switch expressions for piece movement, move results
- **LINQ**: used in `Board` for attack detection and legal move checking
- **IDisposable**: `GameStats` saves data on dispose; `SdlContext` frees native library
- **Generics**: `List<(int, int)>` for valid moves
- **Records/Tuples**: value tuples for board positions and last-move tracking
- **JSON serialization**: game stats persisted to `chess_stats.json`

