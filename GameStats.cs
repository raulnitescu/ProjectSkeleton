using System.Text.Json;

namespace TheAdventure;

//tine statistici si alte jocuri sau loaduri
//salveaza intr-un fisier json in directorul curent
public class GameStats : IDisposable
{
    //path ul catre fisierul de statistici, in directorul curent al aplicatiei
    private static readonly string StatsFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "chess_stats.json");

    public int WhiteWins { get; set; }
    public int BlackWins { get; set; }
    public int Draws { get; set; }
    public int TotalGames { get; set; }

    private bool _disposed;

    //facem load la statistici din fisier, daca exista, altfel cream un nou obiect cu valori default
    public static GameStats Load()
    {
        if (File.Exists(StatsFilePath)) {
            try{
                string json = File.ReadAllText(StatsFilePath);
                var stats = JsonSerializer.Deserialize<GameStats>(json);
                if (stats != null){
                    return stats;
                }
            }
            catch (JsonException){
                Console.WriteLine("Warning: Could not read stats file, starting fresh.");
            }
        }
        return new GameStats();
    }

    //salavam ca json
    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(StatsFilePath, json);
    }

    //actualizam statistici dupa fiecare joc
    public void RecordResult(MoveResult result, PieceColor winner)
    {
        TotalGames++;
        if (result == MoveResult.Stalemate){
            Draws++;
        }
        else if (result == MoveResult.Checkmate){
            if (winner == PieceColor.White)
                WhiteWins++;
            else
                BlackWins++;
        }
        Save();
    }

    //implementam IDisposable pentru a ne asigura ca salvam statistici la final
    public void Dispose()
    {
        if (!_disposed){
            Save();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~GameStats()
    {
        if (!_disposed){
            Save();
        }
    }
}

