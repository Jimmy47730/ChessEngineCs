using Core.Board;
using Xunit;

namespace ChessEngine.Tests
{
    public class BitBoardTests
    {
        [Fact]
        public void SetAndIsSet_Works()
        {
            var b = new BitBoard(0);
            var b2 = b.SetBit(0);
            Assert.True(b2.IsSet(0));
            Assert.False(b.IsSet(0)); // original unchanged
        }

        [Fact]
        public void ClearBit_Works()
        {
            var b = BitBoard.FromSquare(10);
            var b2 = b.ClearBit(10);
            Assert.False(b2.IsSet(10));
            Assert.True(b.IsSet(10));
        }

        [Fact]
        public void ToggleBit_Works()
        {
            var b = new BitBoard(0);
            var b1 = b.ToggleBit(5);
            Assert.True(b1.IsSet(5));
            var b2 = b1.ToggleBit(5);
            Assert.False(b2.IsSet(5));
        }

        [Fact]
        public void PopCount_Lsb_Msb()
        {
            var b = BitBoard.FromSquare(0).SetBit(63);
            Assert.Equal(2, b.PopCount());
            Assert.Equal(0, b.LsbIndex());
            Assert.Equal(63, b.MsbIndex());
        }

        [Fact]
        public void Operators_And_Or_Xor()
        {
            var a = BitBoard.FromSquare(1).SetBit(3);
            var c = BitBoard.FromSquare(3);
            Assert.True((a & c).IsSet(3));
            Assert.False((a & c).IsSet(1));
            Assert.True((a | c).IsSet(1));
            Assert.True((a ^ c).IsSet(1));
            Assert.False((a ^ c).IsSet(3));
        }
    }
}
