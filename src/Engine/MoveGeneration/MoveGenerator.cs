using System.Net.NetworkInformation;
using Core.DataStructures;
using Core.Helpers;

namespace Engine.MoveGeneration;

public class MoveGenerator
{
    public static List<Move> GenerateMoves(Board board)
    {
        List<Move> moves = [];
        ulong checkingPieces = CheckRestriction.GetCheckingPieces(board);
        ulong allowedDestinations = Bitboards.FULL;

        // If there are 2 checking pieces (or more in a made up position), the king must move
        if (Operations.PopCount(checkingPieces) >= 2)
            return GenerateKingMoves(board, true);

        if (Operations.PopCount(checkingPieces) == 1)
        {
            moves.AddRange(GenerateKingMoves(board, true));

            allowedDestinations = Operations.GetRay(board.GetBitboard(PieceType.King, board.sideToMove), checkingPieces);

            // If there is no ray between, only moves are captures (knight checks)
            if (allowedDestinations == 0) allowedDestinations = checkingPieces;
        }

        moves.AddRange(GenerateKingMoves(board, false));

        // Other pieces' move generation
        List<ulong> pinMasks = CheckRestriction.GetPinnedPieceMasks(board);
        moves.AddRange(GeneratePawnMoves(board, allowedDestinations, pinMasks));
        moves.AddRange(GenerateKnightMoves(board, allowedDestinations, pinMasks));
        moves.AddRange(GenerateBishopMoves(board, allowedDestinations, pinMasks));
        moves.AddRange(GenerateRookMoves(board, allowedDestinations, pinMasks));
        moves.AddRange(GenerateQueenMoves(board, allowedDestinations, pinMasks));

        return moves;
    }

    // Individual piece methods
    private static List<Move> GenerateKingMoves(Board board, bool isInCheck)
    {
        List<Move> moves = [];
        int kingSquare = Operations.LsbIndex(board.GetBitboard(PieceType.King, board.sideToMove));
        if (kingSquare == -1)
            return moves;

        ulong opponentCoverage = AttackCoverage.GetFullCoverage(board);
        ulong toSquares = Magics.Instance().GetKingMoves(kingSquare);

        // Regular moves
        toSquares &= ~board.GetSideBitboard(board.sideToMove);
        toSquares &= ~opponentCoverage;

        moves.AddRange(CreateMoves(kingSquare, toSquares, board.GetSideBitboard(1 - board.sideToMove)));

        if (isInCheck)
            return moves;

        moves.AddRange(CreateCastlingMoves(
            kingSquare,
            board.GetFullBitboard(),
            board.GetBitboard(PieceType.Rook, board.sideToMove),
            opponentCoverage,
            board.GetCastlingRights()
        ));

        return moves;
    }

    private static List<Move> GeneratePawnMoves(Board board, ulong allowedDestinations, List<ulong> pinRays)
    {
        List<Move> moves = [];
        ulong pawnBitboard = board.GetBitboard(PieceType.Pawn, board.sideToMove);
        SideColor color = board.sideToMove;
        SideColor oppColor = 1 - board.sideToMove;
        int[] shifts = color == SideColor.White ? [7, 8, 9, 16] : [-9, -8, -7, -16]; // Left, push, right, double push (from white's POV)
        int startSquare;

        while ((startSquare = Operations.PopLSB(ref pawnBitboard)) != -1)
        {
            ulong pinRay = Bitboards.FULL;
            foreach (ulong ray in pinRays)
            {
                if (Operations.IsSet(ray, startSquare))
                {
                    pinRay = ray;
                    break;
                }
            }

            // Left capture
            int endSquare = startSquare + shifts[0];
            ulong endBitboard = Operations.SquareToU64(endSquare) & ~Bitboards.FILE_H;
            if ((endBitboard & board.GetSideBitboard(oppColor)) != 0 && (endBitboard & allowedDestinations & pinRay) != 0)
            {
                if ((endBitboard & (Bitboards.RANK_1 | Bitboards.RANK_8)) != 0)
                    moves.AddRange(CreatePromotionMoves(startSquare, endSquare, isCapture: true));
                else
                    moves.Add(new(startSquare, endSquare, Move.GenerateSpecialMoveFlag(CaptureType.Normal)));
            }

            // Right capture
            endSquare = startSquare + shifts[2];
            endBitboard = Operations.SquareToU64(endSquare) & ~Bitboards.FILE_A;
            if ((endBitboard & board.GetSideBitboard(oppColor)) != 0 && (endBitboard & allowedDestinations & pinRay) != 0)
            {
                if ((endBitboard & (Bitboards.RANK_1 | Bitboards.RANK_8)) != 0)
                    moves.AddRange(CreatePromotionMoves(startSquare, endSquare, isCapture: true));
                else
                    moves.Add(new(startSquare, endSquare, Move.GenerateSpecialMoveFlag(CaptureType.Normal)));
            }

            // Single push
            endSquare = startSquare + shifts[1];
            endBitboard = Operations.SquareToU64(endSquare);
            if ((endBitboard & board.GetFullBitboard()) == 0 && (endBitboard & allowedDestinations & pinRay) != 0)
            {
                if ((endBitboard & (Bitboards.RANK_1 | Bitboards.RANK_8)) != 0)
                    moves.AddRange(CreatePromotionMoves(startSquare, endSquare, isCapture: false));
                else
                    moves.Add(new(startSquare, endSquare));
            }

            // Double push
            ulong targetRank = color == SideColor.White ? Bitboards.RANK_2 : Bitboards.RANK_7;
            ulong startBitboard = Operations.SquareToU64(startSquare);
            if ((startBitboard & targetRank) != 0)
            {
                endSquare = startSquare + shifts[3];
                endBitboard = Operations.SquareToU64(endSquare);
                ulong path = Operations.ShiftByInt(startBitboard, shifts[1]) | endBitboard;
                if ((path & board.GetFullBitboard()) == 0 && (endBitboard & allowedDestinations & pinRay) != 0)
                    moves.Add(new(startSquare, endSquare, Move.GenerateSpecialMoveFlag(true)));
            }

            // En passant
            if (board.enPassantSquare == -1)
                continue;
            
            endSquare = board.enPassantSquare;
            endBitboard = Operations.SquareToU64(endSquare);
            // The captured pawn sits on the square behind the target square, hence the swapped number shifts below
            ulong capturedPawn = 
                color == SideColor.White ?
                Operations.ShiftByInt(endBitboard, -8) :
                Operations.ShiftByInt(endBitboard, 8);

            if ((capturedPawn & board.GetBitboard(PieceType.Pawn, oppColor)) == 0)
                continue;
            
            bool isNeighbour = 
                Operations.ShiftByInt(startBitboard, shifts[0]) == endBitboard ||
                Operations.ShiftByInt(startBitboard, shifts[2]) == endBitboard;
            if (isNeighbour && (endBitboard & allowedDestinations & pinRay) != 0)
            {
                int kingSquare = Operations.LsbIndex(board.GetBitboard(PieceType.King, color));
                if (kingSquare / 8 == startSquare / 8)
                {
                    int direction = kingSquare < startSquare ? 1 : -1;
                    ulong involvedPawns = startBitboard | capturedPawn;
                    ulong opponentRookOrQueen =
                        board.GetBitboard(PieceType.Rook, oppColor) |
                        board.GetBitboard(PieceType.Queen, oppColor);

                    for (int square = kingSquare + direction;
                         square >= 0 && square < 64 && square / 8 == kingSquare / 8;
                         square += direction)
                    {
                        ulong squareBitboard = Operations.SquareToU64(square);
                        if ((squareBitboard & involvedPawns) != 0 ||
                            (squareBitboard & board.GetFullBitboard()) == 0)
                            continue;

                        if ((squareBitboard & opponentRookOrQueen) != 0)
                            continue;

                        break;
                    }

                    // The loop's continue above skips the move only when the first blocker is a rook or queen.
                    bool enPassantExposesKing = false;
                    for (int square = kingSquare + direction;
                         square >= 0 && square < 64 && square / 8 == kingSquare / 8;
                         square += direction)
                    {
                        ulong squareBitboard = Operations.SquareToU64(square);
                        if ((squareBitboard & involvedPawns) != 0 ||
                            (squareBitboard & board.GetFullBitboard()) == 0)
                            continue;

                        enPassantExposesKing = (squareBitboard & opponentRookOrQueen) != 0;
                        break;
                    }

                    if (enPassantExposesKing)
                        continue;
                }
                moves.Add(new(startSquare, endSquare, Move.GenerateSpecialMoveFlag(CaptureType.EnPassant)));   
            }
        }

        return moves;
    }

    private static List<Move> GenerateKnightMoves(Board board, ulong allowedDestinations, List<ulong> pinRays)
    {
        List<Move> moves = [];
        ulong knightBitboard = board.GetBitboard(PieceType.Knight, board.sideToMove);
        int startSquare;

        while ((startSquare = Operations.PopLSB(ref knightBitboard)) != -1)
        {
            bool isPinned = false;
            foreach (ulong ray in pinRays)
            {
                if (Operations.IsSet(ray, startSquare))
                {
                    isPinned = true;
                    break;
                }
            }
            if (isPinned) // Pinned knights can't move in any direction
                break;
            
            ulong allDestinations = Magics.Instance().GetKnightMoves(startSquare);
            allDestinations &= allowedDestinations & ~board.GetSideBitboard(board.sideToMove);

            moves.AddRange(CreateMoves(startSquare, allDestinations, board.GetSideBitboard(1 - board.sideToMove)));
        }

        return moves;
    }

    private static List<Move> GenerateBishopMoves(Board board, ulong allowedDestinations, List<ulong> pinRays)
    {
        List<Move> moves = [];
        ulong bishopBitboard = board.GetBitboard(PieceType.Bishop, board.sideToMove);
        int startSquare;

        while ((startSquare = Operations.PopLSB(ref bishopBitboard)) != -1)
        {
            ulong pinRay = Bitboards.FULL;
            foreach (ulong ray in pinRays)
            {
                if (Operations.IsSet(ray, startSquare))
                {
                    pinRay = ray;
                    break;
                }
            }

            ulong allDestinations = Magics.Instance().GetBishopMoves(startSquare, board.GetFullBitboard());
            allDestinations &= allowedDestinations & pinRay & ~board.GetSideBitboard(board.sideToMove);

            moves.AddRange(CreateMoves(startSquare, allDestinations, board.GetSideBitboard(1 - board.sideToMove)));
        }

        return moves;
    }

    private static List<Move> GenerateRookMoves(Board board, ulong allowedDestinations, List<ulong> pinRays)
    {
        List<Move> moves = [];
        ulong rookBitboard = board.GetBitboard(PieceType.Rook, board.sideToMove);
        int startSquare;

        while ((startSquare = Operations.PopLSB(ref rookBitboard)) != -1)
        {
            ulong pinRay = Bitboards.FULL;
            foreach (ulong ray in pinRays)
            {
                if (Operations.IsSet(ray, startSquare))
                {
                    pinRay = ray;
                    break;
                }
            }

            ulong allDestinations = Magics.Instance().GetRookMoves(startSquare, board.GetFullBitboard());
            allDestinations &= allowedDestinations & pinRay & ~board.GetSideBitboard(board.sideToMove);

            moves.AddRange(CreateMoves(startSquare, allDestinations, board.GetSideBitboard(1 - board.sideToMove)));
        }

        return moves;
    }

    private static List<Move> GenerateQueenMoves(Board board, ulong allowedDestinations, List<ulong> pinRays)
    {
        List<Move> moves = [];
        ulong queenBitboard = board.GetBitboard(PieceType.Queen, board.sideToMove);
        int startSquare;

        while ((startSquare = Operations.PopLSB(ref queenBitboard)) != -1)
        {
            ulong pinRay = Bitboards.FULL;
            foreach (ulong ray in pinRays)
            {
                if (Operations.IsSet(ray, startSquare))
                {
                    pinRay = ray;
                    break;
                }
            }

            ulong allDestinations = Magics.Instance().GetQueenMoves(startSquare, board.GetFullBitboard());
            allDestinations &= allowedDestinations & pinRay & ~board.GetSideBitboard(board.sideToMove);

            moves.AddRange(CreateMoves(startSquare, allDestinations, board.GetSideBitboard(1 - board.sideToMove)));
        }

        return moves;
    }


    // Helper methods
    private static List<Move> CreateMoves(int from, ulong to, ulong opponentPieces, int flags = 0)
    {
        List<Move> moves = [];
        int toSquare;
        while ((toSquare = Operations.PopLSB(ref to)) != -1)
        {
            int moveFlags = flags;
            if (moveFlags == 0 && (Operations.SquareToU64(toSquare) & opponentPieces) != 0)
                moveFlags = Move.GenerateSpecialMoveFlag(CaptureType.Normal);

            moves.Add(new(from, toSquare, moveFlags));
        }

        return moves;
    }

    private static List<Move> CreatePromotionMoves(int from, int to, bool isCapture)
    {
        List<Move> moves = [];
                
        for (int i = 1; i <= 4; i++)
            moves.Add(new(from, to, Move.GeneratePromotionFlag((PieceType)i, isCapture)));

        return moves;
    }

    private static List<Move> CreateCastlingMoves(
        int from,
        ulong occupancy,
        ulong rookBitboard,
        ulong coverage,
        bool[] castlingRights
    )
    {
        List<Move> moves = [];
        if (from != 4 && from != 60)
            return moves;

        // Shifts are the same for each side and represents the squares between the king and rook.
        // The first two numbers have to be empty and not attacked, while the third square (queen side only) just needs to be empty.
        int[] kingShifts = [1, 2];
        int[] queenShifts = [-1, -2, -3];
        int sideToMove = from == 4 ? 0 : 1; // Represented as integer to make calculations easier for this method

        Move kingSideCastle = CreateKingSideCastle();
        if (kingSideCastle.IsValid())
            moves.Add(kingSideCastle);

        Move queenSideCastle = CreateQueenSideCastle();
        if (queenSideCastle.IsValid())
            moves.Add(queenSideCastle);

        // Internal helper methods to create the castling moves
        Move CreateKingSideCastle()
        {
            if (!castlingRights[2 * sideToMove])
                return Move.Null();
            if ((rookBitboard & Operations.SquareToU64(from + 3)) == 0)
                return Move.Null();

            int through = from + kingShifts[0];
            int to = from + kingShifts[1];
            ulong neededClearance = Operations.SquareToU64(through) | Operations.SquareToU64(to);

            if ((neededClearance & (occupancy | coverage)) != 0)
                return Move.Null();

            return new(from, to, Move.GenerateSpecialMoveFlag(CastlingType.KingSide));
        }
        Move CreateQueenSideCastle()
        {
            if (!castlingRights[2 * sideToMove + 1])
                return Move.Null();
            if ((rookBitboard & Operations.SquareToU64(from - 4)) == 0)
                return Move.Null();

            int through = from + queenShifts[0];
            int to = from + queenShifts[1];
            int extra = from + queenShifts[2];
            ulong neededClearance = Operations.SquareToU64(through) | Operations.SquareToU64(to);

            if ((neededClearance & (occupancy | coverage)) != 0)
                return Move.Null();
            if ((Operations.SquareToU64(extra) & occupancy) != 0)
                return Move.Null();

            return new(from, to, Move.GenerateSpecialMoveFlag(CastlingType.QueenSide));
        }

        return moves;
    }
}