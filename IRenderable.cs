namespace TheAdventure;

//interfata pentru obiectele care pot fi desenate pe ecran
public interface IRenderable
{
    //deseneaza obiectul pe ecran la un loc specificat
    void Render(IntPtr renderer, Silk.NET.SDL.Sdl sdl, int pixelX, int pixelY, int squareSize);
}

