using System.Diagnostics;
using Silk.NET.SDL;
using Silk.NET.Maths;
namespace TheAdventure;
public static class Program
{
    //fiecare patrat are o latura de 100 de pixeli
    private const int SquareSize = 100;
    private const int BoardOffset = 0;
    // deci la final tot window ul o sa aiba (8 squares * 100 pixels, 800x800)
    private const int WindowSize = 800;
    public static void Main()
    {
        var sdl = new Sdl(new SdlContext());
        var sdlInitResult = sdl.Init(Sdl.InitVideo | Sdl.InitEvents | Sdl.InitTimer);
        if (sdlInitResult < 0)
        {
            throw new InvalidOperationException("Failed to initialize SDL.");
        }
        //creez window ul
        IntPtr window;
        unsafe
        {
            window = (IntPtr)sdl.CreateWindow(
                "Chess Game", Sdl.WindowposUndefined, Sdl.WindowposUndefined,
                WindowSize, WindowSize,
                (uint)WindowFlags.Shown
            );
            if (window == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create window.");
            }
        }
        //creez renderer ul
        IntPtr renderer;
        unsafe
        {
            renderer = (IntPtr)sdl.CreateRenderer((Window*)window, -1, (uint)RendererFlags.Accelerated);
            sdl.RenderSetVSync((Renderer*)renderer, 1);
        }
        if (renderer == IntPtr.Zero){
            throw new InvalidOperationException("Failed to create renderer.");
        }
        //initializare joc
        var board = new Board();
        board.SetupStartingPosition();
        //dam load la stats din fisier
        using var stats = GameStats.Load();
        Console.WriteLine("=== Chess Game ===");
        Console.WriteLine($"Stats: White wins={stats.WhiteWins}, Black wins={stats.BlackWins}, Draws={stats.Draws}");
        Console.WriteLine("Click a piece to select it, then click a destination to move.");
        Console.WriteLine("Press R to restart, ESC to quit.");
        int selectedRow = -1;
        int selectedCol = -1;
        //verificam daca jocul s a terminat (checkmate sau stalemate)
        bool gameOver = false;
        string statusMessage = "White's turn";
        //lista de mutari valide pentru piesa selectata (pentru highlight)
        var validMoves = new List<(int row, int col)>();
        var ev = new Event();
        var timer = new Stopwatch();
        bool quit = false;
        //main game loop
        while (!quit)
        {
            //aici procesam evenimentele (input)
            while (sdl.PollEvent(ref ev) != 0)
            {
                switch (ev.Type)
                {
                    case (uint)EventType.Quit:
                        quit = true;
                        break;
                    case (uint)EventType.Keydown:
                        HandleKeyDown(ev, ref quit, ref gameOver, board, ref selectedRow,
                            ref selectedCol, ref statusMessage, validMoves);
                        break;
                    case (uint)EventType.Mousebuttondown:
                        if (!gameOver && ev.Button.Button == (byte)MouseButton.Primary)
                        {
                            HandleMouseClick(ev.Button.X, ev.Button.Y, board, stats,
                                ref selectedRow, ref selectedCol, ref gameOver,
                                ref statusMessage, validMoves);
                        }
                        break;
                }
            }
            //aici facem update la starea jocului (daca e necesar) - in acest caz logica de joc e deja in HandleMouseClick, deci nu avem nevoie de update separat
            timer.Restart();
            //aici facem render la scena
            RenderFrame(sdl, renderer, board, selectedRow, selectedCol, validMoves);
        }
        //cleanup
        unsafe
        {
            sdl.DestroyRenderer((Renderer*)renderer);
            sdl.DestroyWindow((Window*)window);
        }
        sdl.Quit();
    }
    //manager de key input
    private static void HandleKeyDown(Event ev, ref bool quit, ref bool gameOver,
        Board board, ref int selectedRow, ref int selectedCol,
        ref string statusMessage, List<(int row, int col)> validMoves)
    {
        var key = (KeyCode)ev.Key.Keysym.Scancode;
        switch (key)
        {
            case KeyCode.Escape:
                quit = true;
                break;
            case KeyCode.R:
                //restrart la joc
                board.SetupStartingPosition();
                selectedRow = -1;
                selectedCol = -1;
                gameOver = false;
                statusMessage = "White's turn";
                validMoves.Clear();
                Console.WriteLine("Game restarted!");
                break;
        }
    }
    //daca dai click pe o piesa sa vezi mutarile posibile
    private static void HandleMouseClick(int mouseX, int mouseY, Board board, GameStats stats,
        ref int selectedRow, ref int selectedCol, ref bool gameOver,
        ref string statusMessage, List<(int row, int col)> validMoves)
    {
        int clickedCol = (mouseX - BoardOffset) / SquareSize;
        int clickedRow = (mouseY - BoardOffset) / SquareSize;
        if (clickedRow < 0 || clickedRow > 7 || clickedCol < 0 || clickedCol > 7) return;
        if (selectedRow < 0){
            var piece = board.GetPiece(clickedRow, clickedCol);
            if (piece != null && piece.Color == board.CurrentTurn){
                selectedRow = clickedRow;
                selectedCol = clickedCol;
                //calculam mutarile valide pentru piesa selectata
                validMoves.Clear();
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (board.IsValidMove(selectedRow, selectedCol, r, c))
                        {
                            validMoves.Add((r, c));
                        }
                    }
                }
                Console.WriteLine($"Selected {piece.GetLabel()} at ({clickedRow}, {clickedCol})");
            }
        }
        else
        {
            //o piesa e deja selectata, deci incercam sa mutam
            if (board.IsValidMove(selectedRow, selectedCol, clickedRow, clickedCol))
            {
                var result = board.MakeMove(selectedRow, selectedCol, clickedRow, clickedCol);
                //update la status message in functie de rezultat
                statusMessage = result switch
                {
                    MoveResult.Normal => $"{board.CurrentTurn}'s turn",
                    MoveResult.Check => $"{board.CurrentTurn} is in CHECK!",
                    MoveResult.Checkmate => DetermineCheckmateMessage(board, stats),
                    MoveResult.Stalemate => "STALEMATE - It's a draw!",
                    _ => statusMessage
                };
                if (result is MoveResult.Checkmate or MoveResult.Stalemate)
                {
                    gameOver = true;
                    Console.WriteLine(statusMessage);
                    Console.WriteLine("Press R to restart or ESC to quit.");
                }
                Console.WriteLine($"Move result: {result}");
            }
            else
            {
                //daca dai click pe piesa ta te muti pe ea si vezi mutarile posibile
                var piece = board.GetPiece(clickedRow, clickedCol);
                if (piece != null && piece.Color == board.CurrentTurn)
                {
                    selectedRow = clickedRow;
                    selectedCol = clickedCol;
                    validMoves.Clear();
                    for (int r = 0; r < 8; r++)
                    {
                        for (int c = 0; c < 8; c++)
                        {
                            if (board.IsValidMove(selectedRow, selectedCol, r, c))
                            {
                                validMoves.Add((r, c));
                            }
                        }
                    }
                    return;
                }
            }
            //curatam selectia daca nu am mutat sau nu am selectat alta piesa
            selectedRow = -1;
            selectedCol = -1;
            validMoves.Clear();
        }
    }
    //helper pentru mesajul de checkmate si update la statistici
    private static string DetermineCheckmateMessage(Board board, GameStats stats)
    {
        //castigaotrul e cel care nu e la mutare, pentru ca cel care e la mutare e cel care a fost dat checkmate
        var winner = board.CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        stats.RecordResult(MoveResult.Checkmate, winner);
        return $"CHECKMATE! {winner} wins!";
    }
    //desenam tabla, piesele, highlight pentru mutarile posibile si status message
    private static void RenderFrame(Sdl sdl, IntPtr renderer, Board board,
        int selectedRow, int selectedCol, List<(int row, int col)> validMoves)
    {
        unsafe
        {
            var r = (Renderer*)renderer;
            //curatam ecranul
            sdl.SetRenderDrawColor(r, 48, 48, 48, 255);
            sdl.RenderClear(r);
            //desenam tabla
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    int px = BoardOffset + col * SquareSize;
                    int py = BoardOffset + row * SquareSize;
                    //alternam culorile pentru a crea modelul de tabla
                    bool isLightSquare = (row + col) % 2 == 0;
                    if (isLightSquare)
                    {
                        sdl.SetRenderDrawColor(r, 240, 217, 181, 255); // light tan
                    }
                    else
                    {
                        sdl.SetRenderDrawColor(r, 181, 136, 99, 255); // dark brown
                    }
                    var squareRect = new Rectangle<int>(new Vector2D<int>(px, py), new Vector2D<int>(SquareSize, SquareSize));
                    sdl.RenderFillRect(r, &squareRect);
                    //desenam piesa daca exista
                    if (row == selectedRow && col == selectedCol)
                    {
                        sdl.SetRenderDrawColor(r, 255, 255, 0, 255);
                        sdl.RenderDrawRect(r, &squareRect);
                        var innerRect = new Rectangle<int>(new Vector2D<int>(px + 1, py + 1), new Vector2D<int>(SquareSize - 2, SquareSize - 2));
                        sdl.RenderDrawRect(r, &innerRect);
                        var innerRect2 = new Rectangle<int>(new Vector2D<int>(px + 2, py + 2), new Vector2D<int>(SquareSize - 4, SquareSize - 4));
                        sdl.RenderDrawRect(r, &innerRect2);
                    }
                    //highlight pentru mutarile posibile in verde
                    if (validMoves.Exists(m => m.row == row && m.col == col))
                    {
                        sdl.SetRenderDrawColor(r, 0, 200, 0, 255);
                        int dotSize = 16;
                        var dotRect = new Rectangle<int>(
                            new Vector2D<int>(px + (SquareSize - dotSize) / 2, py + (SquareSize - dotSize) / 2),
                            new Vector2D<int>(dotSize, dotSize)
                        );
                        sdl.RenderFillRect(r, &dotRect);
                    }
                    //desenam piesa daca exista
                    var piece = board.GetPiece(row, col);
                    piece?.Render(renderer, sdl, px, py, SquareSize);
                }
            }
            sdl.RenderPresent(r);
        }
    }
}
