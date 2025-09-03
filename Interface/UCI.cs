using Engine;
using Core.Helpers;
namespace Chess;

public class UCI
{
    private readonly Bot player;

	static readonly string[] positionLabels = ["fen", "moves"];
	static readonly string[] goLabels = ["movetime", "wtime", "btime", "winc", "binc", "movestogo"];

    public UCI()
    {
        player = new Bot();
        player.OnMoveChosen = OnMoveChosen;
    }

    public void ReceiveCommand(string message)
    {
        Logger.Info("Command received: " + message);
        message = message.Trim();
        string messageType = message.Split(' ')[0].ToLower();

        switch (messageType)
        {
            case "uci":
                Respond("uciok");
                break;
            case "isready":
                Respond("readyok");
                break;
            case "ucinewgame":
                player.NotifyNewGame();
                break;
            case "position":
                ProcessPositionCommand(message);
                break;
            case "go":
                ProcessGoCommand(message);
                break;
            case "stop":
                if (player.IsThinking)
                {
                    player.StopThinking();
                }
                break;
            case "quit":
                player.Quit();
                break;
            case "d":
                Console.WriteLine(player.GetBoardDiagram());
                break;
            default:
                Logger.Warn($"Unrecognized command: {messageType}");
                break;
        }
    }

    public static void OnMoveChosen(string bestMove)
    {
        Respond($"bestmove {bestMove}");
    }

    private void ProcessGoCommand(string message)
    {
        message = message.Trim();

        // Checks for a defined thinking time
        if (message.Contains("movetime"))
        {
            int moveTime = int.Parse(GetLabelledValue(message, "movetime", goLabels));
            Logger.Info($"Received 'movetime' command with {moveTime} ms");
            player.Think(moveTime);
            return;
        }

        if (!message.Contains("wtime") || !message.Contains("btime"))
        {
            Logger.Warn("Missing required time control parameters in 'go' command.");
            Respond("bestmove 0000");
            return;
        }

        int whiteTime = int.Parse(GetLabelledValue(message, "wtime", goLabels));
        int blackTime = int.Parse(GetLabelledValue(message, "btime", goLabels));

        int whiteInc = 0;
        int blackInc = 0;
        if (message.Contains("winc") || message.Contains("binc"))
        {
            whiteInc = int.Parse(GetLabelledValue(message, "winc", goLabels));
            blackInc = int.Parse(GetLabelledValue(message, "binc", goLabels));
        }

        int time = player.DefineThinkingTime(whiteTime, blackTime, whiteInc, blackInc);
        Logger.Info($"Allocated thinking time: {time} ms");
        player.Think(time);
    }

    private void ProcessPositionCommand(string message)
    {
        message = message.Trim();
        if (message.Contains("startpos"))
        {
            player.SetPosition(FenUtility.defaultFen);
        }
        else if (message.Contains("fen"))
        {
            string fen = GetLabelledValue(message, "fen", positionLabels);
            if (string.IsNullOrWhiteSpace(fen))
            {
                Logger.Warn("FEN string is empty.");
                return;
            }
            player.SetPosition(fen);
        }
        else
        {
            Logger.Warn("Unrecognized position command (expected 'startpos' or 'fen'). Found: " + message);
        }

        // Handle additional moves
        string movesStr = GetLabelledValue(message, "moves", positionLabels);
        string[] moves = movesStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var move in moves)
        {
            player.ApplyMove(move);
        }
    }

    private static void Respond(string response)
    {
        Console.WriteLine(response);
        Logger.Info("Response sent: " + response);
    }

    private static string GetLabelledValue(string message, string label, string[] allLabels)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(label)) return string.Empty;
        if (!message.Contains(label)) return string.Empty;

        int labelIndex = message.IndexOf(label);
        if (labelIndex == -1) return string.Empty;

        int valueStart = labelIndex + label.Length;
        int valueEnd = message.Length;

        // Find the earliest occurrence of any label after valueStart
        foreach (var l in allLabels)
        {
            if (l == label) continue;
            int index = message.IndexOf(l, valueStart, StringComparison.Ordinal);
            if (index != -1 && index < valueEnd)
            {
                valueEnd = index;
            }
        }

        string value = message[valueStart..valueEnd].Trim();

        Logger.Debug($"Extracted value for '{label}': {value}");
        return value;
    }
}