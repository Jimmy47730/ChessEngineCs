using Core.Board;
using Core.Helpers;
using Engine.MoveGeneration;
using System;
using System.Collections.Generic;
using Engine;
using Xunit;

namespace ChessEngine.Tests;

public class MoveGenTests
{
    private Dictionary<string, Dictionary<int, int>> expectedPerftCounts = new()
    {
        { "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", new Dictionary<int, int> { { 1, 20 }, { 2, 400 }, { 3, 8902 }, { 4, 197281 } } },
    };

    [Fact]
    public void PerftTests()
    {
        foreach (var (fen, expectedCounts) in expectedPerftCounts)
        {
            BoardState board = new(fen);
            foreach (var (depth, expectedCount) in expectedCounts)
            {
                Assert.Equal(expectedCount, GetPerftResults(board, depth).Count);
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