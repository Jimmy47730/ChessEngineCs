using System.Reflection;
using Chess;
using Core.DataStructures;
using Engine;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ChessEngine.Tests.Interface;

public class UciCommandTests : IClassFixture<UciLoggingFixture>
{
    public UciCommandTests(UciLoggingFixture _)
    {
    }

    [Fact]
    public void UciCommand_RespondsWithProtocolIdentification()
    {
        Assert.Equal("uciok" + Environment.NewLine, CaptureOutput(() => new UCI().ReceiveCommand("  UCI  ")));
    }

    [Fact]
    public void IsReadyCommand_RespondsReady()
    {
        Assert.Equal("readyok" + Environment.NewLine, CaptureOutput(() => new UCI().ReceiveCommand("isready")));
    }

    [Fact]
    public void NewGameCommand_ResetsToTheStartingPosition()
    {
        var uci = new UCI();

        string output = CaptureOutput(() =>
        {
            uci.ReceiveCommand("ucinewgame");
            uci.ReceiveCommand("d");
        });

        Assert.Contains("Fen         : rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", output);
    }

    [Fact]
    public void PositionStartposCommand_SetsTheStartingPosition()
    {
        var uci = new UCI();

        string output = CaptureOutput(() =>
        {
            uci.ReceiveCommand("position startpos");
            uci.ReceiveCommand("d");
        });

        Assert.Contains("Fen         : rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", output);
    }

    [Fact]
    public void PositionFenCommand_SetsTheGivenPosition()
    {
        var uci = new UCI();
        const string fen = "4k3/8/8/8/8/8/8/4K3 b - - 3 7";

        string output = CaptureOutput(() =>
        {
            uci.ReceiveCommand($"position fen {fen}");
            uci.ReceiveCommand("d");
        });

        Assert.Contains($"Fen         : {fen}", output);
    }

    [Fact]
    public void PositionCommandWithEmptyFenDoesNotSetPositionOrWriteOutput()
    {
        var uci = new UCI();

        Assert.Empty(CaptureOutput(() => uci.ReceiveCommand("position fen")));
    }

    [Fact]
    public void PositionCommandWithMalformedFenPropagatesParsingError()
    {
        var uci = new UCI();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            uci.ReceiveCommand("position fen x/8/8/8/8/8/8/4K3 w - - 0 1"));
    }

    [Fact]
    public void PositionCommandWithUnknownModeDoesNotWriteOutput()
    {
        var uci = new UCI();

        Assert.Empty(CaptureOutput(() => uci.ReceiveCommand("position unknown")));
    }

    [Fact]
    public void GoMovetimeCommand_ProducesBestMove()
    {
        var uci = new UCI();
        uci.ReceiveCommand("position startpos");

        string output = CaptureOutput(() => uci.ReceiveCommand("go movetime 25"));

        Assert.StartsWith("bestmove ", output);
    }

    [Fact]
    public void GoClockCommand_ProducesBestMove()
    {
        var uci = new UCI();
        uci.ReceiveCommand("position startpos");

        string output = CaptureOutput(() => uci.ReceiveCommand("go wtime 1000 btime 1000 winc 10 binc 20"));

        Assert.StartsWith("bestmove ", output);
    }

    [Fact]
    public void GoClockCommand_AllowsOptionalIncrementsAndMovesToGo()
    {
        var uci = new UCI();
        uci.ReceiveCommand("position startpos");

        string output = CaptureOutput(() => uci.ReceiveCommand("go wtime 1000 btime 1000 movestogo 5"));

        Assert.StartsWith("bestmove ", output);
    }

    [Fact]
    public void GoWithoutRequiredClocksRespondsWithNoMove()
    {
        var uci = new UCI();

        Assert.Equal("bestmove 0000" + Environment.NewLine, CaptureOutput(() => uci.ReceiveCommand("go")));
    }

    [Fact]
    public void GoWithInvalidMovetimePropagatesFormatError()
    {
        var uci = new UCI();

        Assert.Throws<FormatException>(() => uci.ReceiveCommand("go movetime invalid"));
    }

    [Fact]
    public void GoWithOneIncrementAndMissingOtherIncrementPropagatesFormatError()
    {
        var uci = new UCI();

        Assert.Throws<FormatException>(() => uci.ReceiveCommand("go wtime 1000 btime 1000 winc 10"));
    }

    [Fact]
    public void StopCommandDoesNothingWhenTheBotIsNotThinking()
    {
        var uci = new UCI();

        Assert.Empty(CaptureOutput(() => uci.ReceiveCommand("stop")));
    }

    [Fact]
    public void StopCommandCallsStopThinkingAndPropagatesItsErrorWhenThinking()
    {
        var uci = new UCI();
        var player = (Bot)typeof(UCI)
            .GetField("player", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(uci)!;
        typeof(Bot).GetProperty(nameof(Bot.IsThinking))!.SetValue(player, true);

        Assert.Throws<NotImplementedException>(() => uci.ReceiveCommand("stop"));
    }

    [Fact]
    public void QuitCommandCallsQuitAndPropagatesItsError()
    {
        Assert.Throws<NotImplementedException>(() => new UCI().ReceiveCommand("quit"));
    }

    [Fact]
    public void DisplayCommandWritesTheBoardDiagram()
    {
        var uci = new UCI();
        uci.ReceiveCommand("position startpos");

        string output = CaptureOutput(() => uci.ReceiveCommand("d"));

        Assert.Contains("| r ", output);
        Assert.Contains("Fen         : rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", output);
    }

    [Fact]
    public void UnknownAndEmptyCommandsDoNotWriteOutput()
    {
        var uci = new UCI();

        Assert.Empty(CaptureOutput(() => uci.ReceiveCommand("unknown")));
        Assert.Empty(CaptureOutput(() => uci.ReceiveCommand("")));
    }

    [Fact]
    public void OnMoveChosenWritesBestmoveResponse()
    {
        Assert.Equal("bestmove e2e4" + Environment.NewLine, CaptureOutput(() => UCI.OnMoveChosen("e2e4")));
    }

    private static string CaptureOutput(Action action)
    {
        TextWriter original = Console.Out;
        using StringWriter output = new();
        try
        {
            Console.SetOut(output);
            action();
            return output.ToString();
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}

public sealed class UciLoggingFixture : IDisposable
{
    private readonly string logDirectory;
    private readonly string? previousLogDirectory;

    public UciLoggingFixture()
    {
        previousLogDirectory = Environment.GetEnvironmentVariable("CHESS_ENGINE_LOG_DIR");
        logDirectory = Path.Combine(Path.GetTempPath(), $"ChessEngine.Tests.{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable("CHESS_ENGINE_LOG_DIR", logDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(logDirectory))
        {
            Directory.Delete(logDirectory, recursive: true);
        }

        Environment.SetEnvironmentVariable("CHESS_ENGINE_LOG_DIR", previousLogDirectory);
    }
}
