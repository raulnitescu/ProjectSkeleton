using Silk.NET.SDL;
using Silk.NET.Maths;
namespace TheAdventure;
//reprezinta o piesa de sah, cu tipul si culoarea ei
//de asemenea, aici se genereaz cum arata piesele de sah, simplist dar oarecum natural
public class ChessPiece : IRenderable
{
    public PieceType Type { get; }
    public PieceColor Color { get; }
    //verfic daca piesa a fost mutata macar o data, pentru a sti daca poate face en passant sau castling`
    public bool HasMoved { get; set; }
    public ChessPiece(PieceType type, PieceColor color){
        Type = type;
        Color = color;
        HasMoved = false;
    }
    //returneaza o eticheta scurta pentru tipul piesei, folosita in notatia de sah
    public string GetLabel() => Type switch{
        PieceType.King => "K",
        PieceType.Queen => "Q",
        PieceType.Rook => "R",
        PieceType.Bishop => "B",
        PieceType.Knight => "N",
        PieceType.Pawn => "P",
        _ => "?"
    };
    //deseneaza piesa pe tabla de sahcum am zis sus
    public void Render(IntPtr renderer, Sdl sdl, int pixelX, int pixelY, int squareSize)
    {
        unsafe
        {
            var r = (Renderer*)renderer;
            //setnam culoare
            if (Color == PieceColor.White){
                sdl.SetRenderDrawColor(r, 220, 220, 255, 255); //light blue-white
            }
            else{
                sdl.SetRenderDrawColor(r, 180, 50, 50, 255); //dark red
            }
            //merimi diferite
            int pieceSize = Type switch
            {
                PieceType.Pawn => squareSize / 3,
                PieceType.Knight => squareSize * 2 / 5,
                PieceType.Bishop => squareSize * 2 / 5,
                PieceType.Rook => squareSize * 2 / 5,
                PieceType.Queen => squareSize / 2,
                PieceType.King => squareSize / 2,
                _ => squareSize / 3
            };
            //punem piesa pe mijloc
            int offsetX = pixelX + (squareSize - pieceSize) / 2;
            int offsetY = pixelY + (squareSize - pieceSize) / 2;
            //desenam un patrat simplu pentru piesa
            var body = new Rectangle<int>(new Vector2D<int>(offsetX, offsetY), new Vector2D<int>(pieceSize, pieceSize));
            sdl.RenderFillRect(r, &body);
            //desenam o margine pentru a o face sa iasa in evidenta
            if (Color == PieceColor.White)
            {
                sdl.SetRenderDrawColor(r, 0, 0, 150, 255);
            }
            else
            {
                sdl.SetRenderDrawColor(r, 255, 200, 200, 255);
            }
            sdl.RenderDrawRect(r, &body);
            //desenam o marca interioara pentru a diferentia vizual piesele
            DrawTypeMarking(r, sdl, offsetX, offsetY, pieceSize);
        }
    }
    //deseneaza o marca specifica pentru fiecare tip de piesa, folosind linii simple
    private unsafe void DrawTypeMarking(Renderer* r, Sdl sdl, int x, int y, int size)
    {
        //setam o culoare contrastanta pentru marcaj
        if (Color == PieceColor.White){
            sdl.SetRenderDrawColor(r, 0, 0, 180, 255);
        }
        else{
            sdl.SetRenderDrawColor(r, 255, 220, 220, 255);
        }
        int cx = x + size / 2; // center x
        int cy = y + size / 2; // center y
        int m = size / 6;      // margin for inner shapes
        switch (Type)
        {
            case PieceType.King:
                //facem un plus pe rege
                sdl.RenderDrawLine(r, cx, y + m, cx, y + size - m);
                sdl.RenderDrawLine(r, x + m, cy, x + size - m, cy);
                break;
            case PieceType.Queen:
                //un x pe regina
                sdl.RenderDrawLine(r, x + m, y + m, x + size - m, y + size - m);
                sdl.RenderDrawLine(r, x + size - m, y + m, x + m, y + size - m);
                //si o linie orizontala pentru a o diferentia de rege
                sdl.RenderDrawLine(r, x + m, cy, x + size - m, cy);
                break;
            case PieceType.Rook:
                //un patrat interior pentru turn
                var innerRect = new Rectangle<int>(new Vector2D<int>(x + m * 2, y + m * 2), new Vector2D<int>(size - m * 4, size - m * 4));
                sdl.RenderDrawRect(r, &innerRect);
                break;
            case PieceType.Bishop:
                //esenam un X pe nebun
                sdl.RenderDrawLine(r, x + m, y + m, x + size - m, y + size - m);
                sdl.RenderDrawLine(r, x + size - m, y + m, x + m, y + size - m);
                break;
            case PieceType.Knight:
                //un L pe cal
                sdl.RenderDrawLine(r, cx, y + m, cx, cy);
                sdl.RenderDrawLine(r, cx, cy, x + size - m, cy);
                break;
            case PieceType.Pawn:
                //deseneaza un punct mic pentru pion
                var dot = new Rectangle<int>(new Vector2D<int>(cx - 2, cy - 2), new Vector2D<int>(4, 4));
                sdl.RenderFillRect(r, &dot);
                break;
        }
    }
}
