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
    [Fact]
    public void TestStartPosition()
    {
        // Arrange
        PseudoLegal moveGen = PseudoLegal.Instance();
        BoardState currentBoard = FenUtility.ReadFen();

        int GetLegalMoveCount(int depth)
        {
            if (depth == 0) return 1;

            List<Move> legalMoves = moveGen.GenerateMoves(currentBoard);
            int numPositions = 0;
            foreach (Move move in legalMoves)
            {
                BoardState oldBoard = currentBoard;
                currentBoard = currentBoard.ApplyMove(move);
                numPositions += GetLegalMoveCount(depth - 1);
                currentBoard = oldBoard;
            }
            return numPositions;
        }

        Assert.Equal(20, GetLegalMoveCount(1));
        Assert.Equal(400, GetLegalMoveCount(2));
    }

    [Fact]
    public void UselessTest()
    {
        Assert.True(true);
    }
}