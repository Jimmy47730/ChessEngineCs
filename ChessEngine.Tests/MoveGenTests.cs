using Core.Board;
using Core.Helpers;
using Engine.MoveGeneration;
using System;
using System.Collections.Generic;
using Engine;
using Chess; // For Logger
using Xunit;

namespace ChessEngine.Tests;

public class MoveGenTests
{
    private readonly Dictionary<string, Dictionary<int, int>> expectedPerftCounts = new()
    {
        { "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", new Dictionary<int, int> { { 1, 20 }, { 2, 400 }, { 3, 8902 }, { 4, 197281 } } },
    };

    [Fact]
    public void PerftTests()
    {
        Logger.Info($"Starting Perft Tests. Log file: {Logger.LogFilePath}");
        foreach (var (fen, expectedCounts) in expectedPerftCounts)
        {
            BoardState board = new(fen);
            foreach (var (depth, expectedCount) in expectedCounts)
            {
                var results = GetPerftResults(board, depth);
                Logger.Log("PERFT", $"Depth {depth}: expected {expectedCount} got {results.Count}");

                foreach (var m in results)
                    Logger.Log($"PERFT-D{depth}", m.ToString());

                Assert.Equal(expectedCount, results.Count);
            }
        }
    }

    [Fact]
    public void UselessTest()
    {
        Assert.True(true);
    }


    public static List<Move> GetPerftResults(BoardState startingBoard, int depth)
    {
        MoveGenerator moveGen = MoveGenerator.Instance();
        List<Move> legalMoves = moveGen.GenerateMoves(startingBoard);
        List<Move> perftResults = [];

        foreach (Move move in legalMoves)
        {
            BoardState newBoard = startingBoard.ApplyMove(move);
            if (depth == 1)
            {
                perftResults.Add(move);
            }
            else
            {
                perftResults.AddRange(GetPerftResults(newBoard, depth - 1));
            }
        }

        return perftResults;
    }
}