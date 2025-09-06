using System;
using System.Numerics;

namespace Core.Board
{
    public readonly struct BitBoard
    {
        /*
            Represents a 64-bit bitboard for chess piece positions.
            Value type, immutable and cheap to copy.
            LSB = a1 (square 0), MSB = h8 (square 63).
        */
        private readonly ulong bits;

        public BitBoard(ulong bits) => this.bits = bits;

        private static void ValidateSquare(int square)
        {
            if ((uint)square > 63)
                throw new ArgumentOutOfRangeException(nameof(square), "Square must be in range 0..63");
        }

        // Methods
        public BitBoard SetBit(int square)
        {
            ValidateSquare(square);
            return new BitBoard(bits | (1UL << square));
        }

        public BitBoard ClearBit(int square)
        {
            ValidateSquare(square);
            return new BitBoard(bits & ~(1UL << square));
        }

        public BitBoard ToggleBit(int square)
        {
            ValidateSquare(square);
            return new BitBoard(bits ^ (1UL << square));
        }

        public readonly bool IsSet(int square)
        {
            ValidateSquare(square);
            return (bits & (1UL << square)) != 0;
        }

        public readonly ulong ToU64() => bits;

        public static BitBoard FromSquare(int square)
        {
            ValidateSquare(square);
            return new BitBoard(1UL << square);
        }

        public readonly int PopCount() => BitOperations.PopCount(bits);

        // Returns index of least-significant set bit, or -1 if empty
        public readonly int LsbIndex()
        {
            if (bits == 0) return -1;
            return BitOperations.TrailingZeroCount(bits);
        }

        // Returns index of most-significant set bit, or -1 if empty
        public readonly int MsbIndex()
        {
            if (bits == 0) return -1;
            return 63 - BitOperations.LeadingZeroCount(bits);
        }

        public readonly BitBoard ShiftLeft(int n)
        {
            if (n >= 64) return new BitBoard(0);
            if (n <= 0) return this;
            return new BitBoard(bits << n);
        }

        public readonly BitBoard ShiftRight(int n)
        {
            if (n >= 64) return new BitBoard(0);
            if (n <= 0) return this;
            return new BitBoard(bits >> n);
        }

        // Returns a BitBoard with all bits set in the rank of the given square
        public static BitBoard RankBitBoard(int square)
        {
            ValidateSquare(square);
            int rank = square / 8;
            ulong mask = 0xFFUL << (8 * rank);
            return new BitBoard(mask);
        }

        // Returns a BitBoard with all bits set in the file of the given square
        public static BitBoard FileBitBoard(int square)
        {
            ValidateSquare(square);
            int file = square % 8;
            ulong mask = 0x0101010101010101UL << file;
            return new BitBoard(mask);
        }

        public static int PopLSB(ref BitBoard bitboard)
        {
            int lsbIndex = bitboard.LsbIndex();
            if (lsbIndex == -1) return -1;
            bitboard = bitboard.ClearBit(lsbIndex);
            return lsbIndex;
        }

        public static int PieceIndex(char piece)
        {
            return piece switch
            {
                'P' => 0,
                'N' => 1,
                'B' => 2,
                'R' => 3,
                'Q' => 4,
                'K' => 5,
                'p' => 6,
                'n' => 7,
                'b' => 8,
                'r' => 9,
                'q' => 10,
                'k' => 11,
                _ => throw new ArgumentOutOfRangeException(nameof(piece), $"Invalid piece character {piece}")
            };
        }


        // Static default values
        public static BitBoard Empty => new(0);
        public static BitBoard Full => new(0xFFFFFFFFFFFFFFFF);
        public static BitBoard RANK_1 => new(0x00000000000000FF);
        public static BitBoard RANK_2 => new(0x000000000000FF00);
        public static BitBoard RANK_7 => new(0x00FF000000000000);
        public static BitBoard RANK_8 => new(0xFF00000000000000);
        public static BitBoard FILE_A => new(0x0101010101010101);
        public static BitBoard FILE_B => new(0x0202020202020202);
        public static BitBoard FILE_G => new(0x4040404040404040);
        public static BitBoard FILE_H => new(0x8080808080808080);

        // Operation overrides

        public static BitBoard operator &(BitBoard a, BitBoard b) => new(a.bits & b.bits);
        public static BitBoard operator |(BitBoard a, BitBoard b) => new(a.bits | b.bits);
        public static BitBoard operator ^(BitBoard a, BitBoard b) => new(a.bits ^ b.bits);
        public static BitBoard operator ~(BitBoard a) => new(~a.bits);

        public static BitBoard operator &(BitBoard a, ulong b) => new(a.bits & b);
        public static BitBoard operator |(BitBoard a, ulong b) => new(a.bits | b);
        public static BitBoard operator ^(BitBoard a, ulong b) => new(a.bits ^ b);

        public static BitBoard operator &(ulong a, BitBoard b) => new(a & b.bits);
        public static BitBoard operator |(ulong a, BitBoard b) => new(a | b.bits);
        public static BitBoard operator ^(ulong a, BitBoard b) => new(a ^ b.bits);

        public static bool operator ==(BitBoard a, BitBoard b) => a.bits == b.bits;
        public static bool operator !=(BitBoard a, BitBoard b) => a.bits != b.bits;

        public static bool operator ==(BitBoard a, int b) => a.bits == (ulong)b;
        public static bool operator !=(BitBoard a, int b) => a.bits != (ulong)b;
        public static bool operator ==(int a, BitBoard b) => (ulong)a == b.bits;
        public static bool operator !=(int a, BitBoard b) => (ulong)a != b.bits;

        public static bool operator ==(BitBoard a, ulong b) => a.bits == b;
        public static bool operator !=(BitBoard a, ulong b) => a.bits != b;
        public static bool operator ==(ulong a, BitBoard b) => a == b.bits;
        public static bool operator !=(ulong a, BitBoard b) => a != b.bits;

        public static implicit operator BitBoard(ulong value) => new(value);

        public bool Equals(BitBoard other) => bits == other.bits;
        public override bool Equals(object? obj) => obj is BitBoard b && Equals(b);
        public override int GetHashCode() => bits.GetHashCode();


        public override string ToString()
        {
            char[] board = new char[64];
            for (int i = 0; i < 64; i++)
                board[i] = ((bits & (1UL << i)) != 0) ? 'X' : '.';

            var sb = new System.Text.StringBuilder();
            sb.AppendLine();
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                    sb.Append(board[rank * 8 + file]);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}