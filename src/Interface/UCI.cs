using Engine;
using Core.Helpers;
using Core.DataStructures;
using Engine.MoveGeneration;
using System.Threading.Tasks;

namespace Chess;

public class UCI
{
    private readonly Bot player;
    private Task? searchTask;

	static readonly string[] positionLabels = ["fen", "moves"];
	static readonly string[] goLabels = ["perft", "movetime", "wtime", "btime", "winc", "binc", "movestogo"];

    public UCI()
    {
        player = new Bot();
        player.OnMoveChosen = OnMoveChosen;
    }

    public void ReceiveCommand(string message)
    {
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
                StopAndWaitForSearch();
                player.NotifyNewGame();
                break;
            case "position":
                StopAndWaitForSearch();
                ProcessPositionCommand(message);
                break;
            case "go":
                ProcessGoCommand(message);
                break;
            case "stop":
                StopAndWaitForSearch();
                break;
            case "quit":
                StopAndWaitForSearch();
                player.Quit();
                break;
            case "d":
                StopAndWaitForSearch();
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
        string[] commandParts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length > 1 && string.Equals(commandParts[1], "perft", StringComparison.OrdinalIgnoreCase))
        {
            StopAndWaitForSearch();
            if (commandParts.Length != 3)
                throw new FormatException("The 'go perft' command requires exactly one depth argument.");

            int depth = int.Parse(commandParts[2]);
            if (depth < 1)
                throw new ArgumentOutOfRangeException(nameof(depth), "Perft depth must be at least 1.");

            RunPerftDivide(depth);
            return;
        }

        // Checks for a defined thinking time
        if (message.Contains("movetime"))
        {
            int moveTime = int.Parse(GetLabelledValue(message, "movetime", goLabels));
            StartSearch(moveTime);
            return;
        }
        if (message.Contains("infinite"))
        {
            StartSearch(int.MaxValue);
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

        int time = Bot.DefineThinkingTime(whiteTime, blackTime, whiteInc, blackInc);
        Logger.Info($"Allocated thinking time: {time} ms");
        StartSearch(time);
    }

    private void StartSearch(int time)
    {
        StopAndWaitForSearch();
        searchTask = RunSearchAsync(time);
    }

    private async Task RunSearchAsync(int time)
    {
        try
        {
            await player.ThinkAsync(time);
        }
        catch (Exception ex)
        {
            Logger.Error($"Search failed: {ex}");
        }
    }

    private void StopAndWaitForSearch()
    {
        Task? currentSearch = searchTask;
        if (currentSearch is null)
            return;

        player.StopThinking();
        currentSearch.GetAwaiter().GetResult();
        searchTask = null;
    }

    private void RunPerftDivide(int depth)
    {
        long totalNodes = 0;
        foreach (Move move in MoveGenerator.GenerateMoves(player.currentBoard))
        {
            UndoInfo undoInfo = player.currentBoard.ApplyMove(move);
            long nodes;
            try
            {
                nodes = CountPositions(player.currentBoard, depth - 1);
            }
            finally
            {
                player.currentBoard.UndoMove(move, undoInfo);
            }

            totalNodes += nodes;
            Respond($"{StringRepr.MoveToUci(move)}: {nodes}");
        }

        Respond($"Nodes searched: {totalNodes}");
    }

    private static long CountPositions(Board board, int depth)
    {
        if (depth == 0)
            return 1;

        long positions = 0;
        foreach (Move move in MoveGenerator.GenerateMoves(board))
        {
            UndoInfo undoInfo = board.ApplyMove(move);
            try
            {
                positions += CountPositions(board, depth - 1);
            }
            finally
            {
                board.UndoMove(move, undoInfo);
            }
        }

        return positions;
    }

    private void ProcessPositionCommand(string message)
    {
        message = message.Trim();
        if (message.Contains("startpos"))
        {
            player.SetPosition();
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