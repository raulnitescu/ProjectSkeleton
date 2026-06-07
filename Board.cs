namespace TheAdventure;

//aici fac starea tablei de sah si posibilele mutari
public class Board
{
    //aici fac practic harta de 8 pe 8
    private readonly ChessPiece?[,] _squares = new ChessPiece?[8, 8];
    //incepe albul
    public PieceColor CurrentTurn { get; private set; } = PieceColor.White;
    //urmareaste ultima mutare pentru a putea detecta en passant
    public (int fromRow, int fromCol, int toRow, int toCol)? LastMove { get; private set; }
    //punem toate piesele in pozitia lor standard
    public void SetupStartingPosition()
    {
        //curat tabla, null inseamna ca nu e nicio piesa pe acea casuta
        for (int row = 0; row < 8; row++){
            for (int col = 0; col < 8; col++){
                _squares[row, col] = null;
            }
        }
        //asezam pionii pe randurile 2 si 7
        for (int col = 0; col < 8; col++){
            _squares[1, col] = new ChessPiece(PieceType.Pawn, PieceColor.Black);
            _squares[6, col] = new ChessPiece(PieceType.Pawn, PieceColor.White);
        }
        //asezam piesele mari pe randurile 1 si 8
        PieceType[] backRow = [
            PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
            PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
        ];
        for (int col = 0; col < 8; col++){
            _squares[0, col] = new ChessPiece(backRow[col], PieceColor.Black);
            _squares[7, col] = new ChessPiece(backRow[col], PieceColor.White);
        }

        CurrentTurn = PieceColor.White;
        LastMove = null;
    }
    //get permite apasand click pe o casuta sa vedem ce piesa e acolo, daca e null inseamna ca e goala
    public ChessPiece? GetPiece(int row, int col){
        if (row < 0 || row > 7 || col < 0 || col > 7) return null;
        return _squares[row, col];
    }
    //functie bool ce verifica daca mutarea e valida conform regulilor de sah
    public bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        //prima verificare ar fi daca mutarea e in limitele tablei si daca nu e o mutare de la o casuta la ea insasi
        if (fromRow < 0 || fromRow > 7 || fromCol < 0 || fromCol > 7) return false;
        if (toRow < 0 || toRow > 7 || toCol < 0 || toCol > 7) return false;
        if (fromRow == toRow && fromCol == toCol) return false;
        var piece = _squares[fromRow, fromCol];
        if (piece == null) return false;
        if (piece.Color != CurrentTurn) return false;
        //nu putem captura o piesa de aceeasi culoare
        var target = _squares[toRow, toCol];
        if (target != null && target.Color == piece.Color) return false;

        //fiecare piesa are un pattern de mutare specific, verificam daca mutarea respecta acel pattern
        //pt fiecare piesa exista o functie care verifica daca mutarea e valida pentru acea piesa
        bool validPattern = piece.Type switch{
            PieceType.Pawn => IsValidPawnMove(piece, fromRow, fromCol, toRow, toCol),
            PieceType.Rook => IsValidRookMove(fromRow, fromCol, toRow, toCol),
            PieceType.Knight => IsValidKnightMove(fromRow, fromCol, toRow, toCol),
            PieceType.Bishop => IsValidBishopMove(fromRow, fromCol, toRow, toCol),
            PieceType.Queen => IsValidRookMove(fromRow, fromCol, toRow, toCol) || IsValidBishopMove(fromRow, fromCol, toRow, toCol),
            PieceType.King => IsValidKingMove(piece, fromRow, fromCol, toRow, toCol),
            _ => false
        };
        if (!validPattern) return false;

        //pentru a preveni situatia in care regele e in sah dupa mutare, trebuie sa verificam daca mutarea ar lasa regele in sah
        //asta se face prin a simula mutarea, verifica daca regele e in sah, apoi a anula mutarea
        return !WouldLeaveKingInCheck(fromRow, fromCol, toRow, toCol);
    }

    //daca mutarea e valida, o executam, actualizam starea tablei si schimbam tura
    public MoveResult MakeMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        var piece = _squares[fromRow, fromCol]!;
        var captured = _squares[toRow, toCol];

        //pentru en passant, daca un pion captureaza pe diagonal si nu e nicio piesa acolo, inseamna ca a capturat un pion care a trecut pe langa el
        if (piece.Type == PieceType.Pawn && fromCol != toCol && captured == null){
            //capturam pionul care a trecut pe langa el
            _squares[fromRow, toCol] = null;
            captured = new ChessPiece(PieceType.Pawn, CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White);
        }
        //pentru rocada, daca regele se muta doua casute pe orizontala, inseamna ca face rocada, deci trebuie sa mutam si tura corespunzatoare
        if (piece.Type == PieceType.King && Math.Abs(toCol - fromCol) == 2){
            int rookFromCol = toCol > fromCol ? 7 : 0;
            int rookToCol = toCol > fromCol ? 5 : 3;
            var rook = _squares[fromRow, rookFromCol];
            _squares[fromRow, rookFromCol] = null;
            _squares[fromRow, rookToCol] = rook;
            if (rook != null) rook.HasMoved = true;
        }
        //muta piesa
        _squares[toRow, toCol] = piece;
        _squares[fromRow, fromCol] = null;
        piece.HasMoved = true;

        //actualizam ultima mutare pentru a putea verifica en passant la mutarea urmatoare
        LastMove = (fromRow, fromCol, toRow, toCol);

        //facem auto-promote la regina cand ajungem la ultimul rand cu un pion
        if (piece.Type == PieceType.Pawn && (toRow == 0 || toRow == 7)){
            _squares[toRow, toCol] = new ChessPiece(PieceType.Queen, piece.Color) { HasMoved = true };
        }

        //schimbam cine muta
        var opponent = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        CurrentTurn = opponent;

        //verificam daca e sah mat sau pat dupa fiecare mutare
        if (IsInCheck(opponent))
        {
            if (!HasAnyLegalMove(opponent))
            {
                return MoveResult.Checkmate;
            }
            return MoveResult.Check;
        }

        if (!HasAnyLegalMove(opponent))
        {
            return MoveResult.Stalemate;
        }

        return MoveResult.Normal;
    }
    //returneaza true daca regele de culoarea data e in sah, adica daca poate fi atacat de vreo piesa adversa
    public bool IsInCheck(PieceColor color){
        //gasesc pozitia regelui de culoarea data
        var (kingRow, kingCol) = FindKing(color);
        if (kingRow < 0) return false; //n are voie sa se intample, dar daca nu gasim regele, consideram ca nu e in sah

        //verificam daca vreo piesa adversa poate ataca pozitia regelui
        var opponent = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        return IsSquareAttackedBy(kingRow, kingCol, opponent);
    }
    //returneaza true daca jucatorul de culoarea data are vreo mutare legala disponibila
    //asta se foloseste pentru a verifica sah mat sau pat
    private bool IsSquareAttackedBy(int targetRow, int targetCol, PieceColor attackerColor){
        //parcurgem toate piesele de culoarea atacatoare si verificam daca vreuna poate ataca patratul tinta
        return Enumerable.Range(0, 8)
            .SelectMany(row => Enumerable.Range(0, 8).Select(col => (row, col)))
            .Where(pos => _squares[pos.row, pos.col] != null &&
                          _squares[pos.row, pos.col]!.Color == attackerColor)
            .Any(pos => CanAttack(pos.row, pos.col, targetRow, targetCol));
    }
    //returneaza true daca piesa de la (fromRow, fromCol) poate ataca patratul (toRow, toCol) conform regulilor de miscare ale piesei
    //asta se foloseste pentru a verifica daca regele e in sah, dar si pentru a verifica daca o mutare ar lasa regele in sah
    private bool CanAttack(int fromRow, int fromCol, int toRow, int toCol){
        var piece = _squares[fromRow, fromCol];
        if (piece == null) return false;

        return piece.Type switch{
            PieceType.Pawn => CanPawnAttack(piece, fromRow, fromCol, toRow, toCol),
            PieceType.Rook => IsValidRookMove(fromRow, fromCol, toRow, toCol),
            PieceType.Knight => IsValidKnightMove(fromRow, fromCol, toRow, toCol),
            PieceType.Bishop => IsValidBishopMove(fromRow, fromCol, toRow, toCol),
            PieceType.Queen => IsValidRookMove(fromRow, fromCol, toRow, toCol) ||
                               IsValidBishopMove(fromRow, fromCol, toRow, toCol),
            PieceType.King => Math.Abs(toRow - fromRow) <= 1 && Math.Abs(toCol - fromCol) <= 1,
            _ => false
        };
    }
    //pionii ataca diagonal, altfel decat se mista, deci sunt un caz mai special
    private static bool CanPawnAttack(ChessPiece pawn, int fromRow, int fromCol, int toRow, int toCol){
        int direction = pawn.Color == PieceColor.White ? -1 : 1;
        return toRow == fromRow + direction && Math.Abs(toCol - fromCol) == 1;
    }

    //verifica daca mutarea piesei de la (fromRow, fromCol) la (toRow, toCol) ar lasa regele in sah, asta nu e posibil
    private bool WouldLeaveKingInCheck(int fromRow, int fromCol, int toRow, int toCol)
    {
        //simulam mutarea temporar pentru a vedea daca regele ar fi in sah dupa mutare
        var movingPiece = _squares[fromRow, fromCol];
        var capturedPiece = _squares[toRow, toCol];
        //pentru a simula corect mutarea, trebuie sa luam in considerare si captura en passant, care nu apare ca o captura normala pe tabla
        ChessPiece? enPassantCaptured = null;
        if (movingPiece?.Type == PieceType.Pawn && fromCol != toCol && capturedPiece == null){
            enPassantCaptured = _squares[fromRow, toCol];
            _squares[fromRow, toCol] = null;
        }
        //facem mutarea temporar
        _squares[toRow, toCol] = movingPiece;
        _squares[fromRow, fromCol] = null;

        //e in sah?
        bool inCheck = IsInCheck(CurrentTurn);
        //revenim la starea initiala
        _squares[fromRow, fromCol] = movingPiece;
        _squares[toRow, toCol] = capturedPiece;
        if (enPassantCaptured != null){
            _squares[fromRow, toCol] = enPassantCaptured;
        }
        return inCheck;
    }

    //verifica daca jucatorul de culoarea data are vreo mutare legala disponibila, adica daca poate muta vreo piesa fara sa lase regele in sah
    private bool HasAnyLegalMove(PieceColor color)
    {
        var savedTurn = CurrentTurn;
        CurrentTurn = color;
        bool hasMove = Enumerable.Range(0, 8)
            .SelectMany(r => Enumerable.Range(0, 8).Select(c => (r, c)))
            .Where(from => _squares[from.r, from.c]?.Color == color)
            .Any(from => Enumerable.Range(0, 8)
                .SelectMany(r => Enumerable.Range(0, 8).Select(c => (r, c)))
                .Any(to => IsValidMove(from.r, from.c, to.r, to.c)));

        CurrentTurn = savedTurn;
        return hasMove;
    }

    //gasesc pozitia regelui de culoarea data, daca nu il gasesc, returnez (-1, -1)
    private (int row, int col) FindKing(PieceColor color)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var p = _squares[row, col];
                if (p is { Type: PieceType.King } && p.Color == color)
                {
                    return (row, col);
                }
            }
        }
        return (-1, -1);
    }

    //verifica daca mutarea unui pion de la (fromRow, fromCol) la (toRow, toCol) e valida conform regulilor de miscare ale pionului
    private bool IsValidPawnMove(ChessPiece pawn, int fromRow, int fromCol, int toRow, int toCol)
    {
        //pentru a verifica mutarea unui pion, trebuie sa luam in considerare atat mutarea normala cat si captura, care se fac diferit
        int direction = pawn.Color == PieceColor.White ? -1 : 1;
        int startRow = pawn.Color == PieceColor.White ? 6 : 1;

        //mergem inainte un patrat
        if (toCol == fromCol && toRow == fromRow + direction && _squares[toRow, toCol] == null){
            return true;
        }

        //la prima mutare cu un anume pion poti merge 2 pozitii
        if (toCol == fromCol && toRow == fromRow + 2 * direction && fromRow == startRow &&
            _squares[fromRow + direction, fromCol] == null && _squares[toRow, toCol] == null){
            return true;
        }

        //captura se face diagonal, deci trebuie sa verificam daca acolo e o piesa adversa sau daca e captura en passant
        if (Math.Abs(toCol - fromCol) == 1 && toRow == fromRow + direction){
            // Captura normala
            if (_squares[toRow, toCol] != null){
                return true;
            }

            // En passant
            if (LastMove is var (lFromRow, _, lToRow, lToCol) &&
                lToRow == fromRow && lToCol == toCol &&
                Math.Abs(lFromRow - lToRow) == 2 &&
                _squares[lToRow, lToCol]?.Type == PieceType.Pawn){
                return true;
            }
        }
        return false;
    }

    private bool IsValidRookMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        //tura merge in linii drepte
        if (fromRow != toRow && fromCol != toCol) return false;

        //trebuie sa verificam daca drumul e liber, adica nu sunt piese intre pozitia de start si cea de destinatie
        return IsPathClear(fromRow, fromCol, toRow, toCol);
    }

    private static bool IsValidKnightMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        int rowDiff = Math.Abs(toRow - fromRow);
        int colDiff = Math.Abs(toCol - fromCol);
        //calul merge in L, dar L ul poate fi si pelung si pe lat
        return (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2);
    }

    private bool IsValidBishopMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        //nebunul pe diagonale
        if (Math.Abs(toRow - fromRow) != Math.Abs(toCol - fromCol)) return false;

        return IsPathClear(fromRow, fromCol, toRow, toCol);
    }

    private bool IsValidKingMove(ChessPiece king, int fromRow, int fromCol, int toRow, int toCol)
    {
        int rowDiff = Math.Abs(toRow - fromRow);
        int colDiff = Math.Abs(toCol - fromCol);

        //mutarea normala a regelui e de un patrat in orice directie
        if (rowDiff <= 1 && colDiff <= 1){
            return true;
        }

        //rocada
        if (rowDiff == 0 && colDiff == 2 && !king.HasMoved && !IsInCheck(king.Color)){
            int rookCol = toCol > fromCol ? 7 : 0;
            var rook = _squares[fromRow, rookCol];

            //rook-ul trebuie sa fie o piesa de tip tura care nu a fost mutata inca
            if (rook is not { Type: PieceType.Rook } || rook.HasMoved) return false;

            //trebuie sa verificam daca drumul dintre rege si tura e liber
            int step = toCol > fromCol ? 1 : -1;
            for (int c = fromCol + step; c != rookCol; c += step){
                if (_squares[fromRow, c] != null) return false;
            }

            //recele mnu poate merge intr un patrat atacat
            var opponent = king.Color == PieceColor.White ? PieceColor.Black : PieceColor.White;
            for (int c = fromCol; c != toCol + step; c += step){
                if (IsSquareAttackedBy(fromRow, c, opponent)) return false;
            }

            return true;
        }

        return false;
    }

    //verifica daca drumul dintre (fromRow, fromCol) si (toRow, toCol) e liber, adica nu sunt piese intre ele
    private bool IsPathClear(int fromRow, int fromCol, int toRow, int toCol)
    {
        int rowStep = Math.Sign(toRow - fromRow);
        int colStep = Math.Sign(toCol - fromCol);

        int currentRow = fromRow + rowStep;
        int currentCol = fromCol + colStep;

        while (currentRow != toRow || currentCol != toCol){
            if (_squares[currentRow, currentCol] != null) return false;
            currentRow += rowStep;
            currentCol += colStep;
        }

        return true;
    }
}

//dupa o mutare, poti fi in una din 4 siutuatii
public enum MoveResult
{
    Normal,
    Check,
    Checkmate,
    Stalemate
}

