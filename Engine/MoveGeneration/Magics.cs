using System.Numerics;

namespace Engine.MoveGeneration
{
    public class Magics
    {
        private static Magics instance = new();
        private static readonly object lockObj = new();

        // Magic tables
        private struct SMagic
        {
            public int tableOffset;
            public ulong mask;
            public ulong magic;
            public int shift;
        };

        private readonly SMagic[] bishopMagic = new SMagic[64];
        private readonly SMagic[] rookMagic = new SMagic[64];
        private readonly ulong[] attackTable = new ulong[107648];

        private readonly ulong[] bishopMagicNumbers = [
        0x0040040822862081UL, 0x0004011802028400UL, 0x0014034401000410UL, 0x0008204242840040UL,
        0x488404200a000040UL, 0x0002010420000400UL, 0x20150807042000a0UL, 0x0022010108410402UL,
        0x0000080908218411UL, 0x8228300101010200UL, 0x0005846800810902UL, 0x04000820a0200000UL,
        0x0002840504254104UL, 0x004806091018020cUL, 0x0608508184202004UL, 0x21000c2098280819UL,
        0x2020004242020200UL, 0x4102100490040101UL, 0x0114012208001500UL, 0x0108000682004460UL,
        0x7809000490401000UL, 0x8c02001120900808UL, 0x4024016100821001UL, 0x0041004024050420UL,
        0x084440422002c400UL, 0x0119111094040810UL, 0x4404480810048010UL, 0x042011000802400cUL,
        0x8001001009004000UL, 0x4010108001004128UL, 0x600202009402c204UL, 0x0210848182021280UL,
        0x0012021000c01120UL, 0x001a482a00041020UL, 0x1002404800100930UL, 0x0002008020420200UL,
        0x0020040c0002c102UL, 0x0006080200804050UL, 0x9a82089908440401UL, 0x0038050046102202UL,
        0x0188084884400911UL, 0x0004008249009020UL, 0x1c02001048200401UL, 0x6002520214041a02UL,
        0x2800401091000200UL, 0x0044910051001200UL, 0x2018080860420082UL, 0xd003410101000a02UL,
        0x20088a18208c0040UL, 0x012a010101100100UL, 0x00004241d4100310UL, 0x004000008c240010UL,
        0xd048282060410880UL, 0xcd82401002062100UL, 0x0288c20806240014UL, 0x1820843102002050UL,
        0x008200c844100804UL, 0x01109460a4102800UL, 0x100020004c140400UL, 0x0042070002840404UL,
        0x0000350020046404UL, 0x2400810850030a00UL, 0x01015060a1092212UL, 0x0020202444802040UL
        ];
        private readonly ulong[] rookMagicNumbers = [
            0x7180029224804000UL, 0x6040100020014001UL, 0x5900140900200040UL, 0x8100210038045000UL,
            0x4080040003800800UL, 0x010004005100481aUL, 0x008002000f002080UL, 0xc080004021000480UL,
            0x00808000924001a0UL, 0x0088802001400880UL, 0x0411001042200101UL, 0x0105002100281001UL,
            0x2201000800450010UL, 0x0052800200040081UL, 0x0002000142000884UL, 0xc002000112004084UL,
            0x0040028000204482UL, 0x0020420020820900UL, 0x1002020020804411UL, 0x0028808008001004UL,
            0x0418010009000411UL, 0x540080800c000200UL, 0x0100840058050250UL, 0x8080460000c401a1UL,
            0x020080208001c000UL, 0x00c0008080200448UL, 0x0250002020040802UL, 0x4e01001900245000UL,
            0x1000110100040800UL, 0x2006000200884410UL, 0x0008080400021009UL, 0x0000800080186300UL,
            0x9c80004000402000UL, 0xc008462002401001UL, 0x1003883000802000UL, 0x2010811004800800UL,
            0x1203801400801802UL, 0x0112000502001028UL, 0x0400420304004810UL, 0x2001001041000282UL,
            0x0000400820808000UL, 0x0800200040008080UL, 0x1040c02001010012UL, 0x0050010010210008UL,
            0x0202000890060020UL, 0x2401008804010012UL, 0x0102004408020005UL, 0x1010508100420024UL,
            0x0002410080002900UL, 0x1000208500401100UL, 0x0002402002110100UL, 0x0028100089006100UL,
            0x1060150028001100UL, 0x004201b0080c0a00UL, 0x010a801200010080UL, 0x0200040080411200UL,
            0x0180201089020042UL, 0x3200811108204202UL, 0x11002000108b0041UL, 0x1110300049002005UL,
            0x7310a80005001591UL, 0x000100680c000201UL, 0x200050420188110cUL, 0x014203814021040aUL
        ];
        private readonly ulong[] knightPrecomputedAttacks = [
            0x0000000000020400UL, 0x0000000000050800UL, 0x00000000000a1100UL, 0x0000000000142200UL,
            0x0000000000284400UL, 0x0000000000508800UL, 0x0000000000a01000UL, 0x0000000000402000UL,
            0x0000000002040004UL, 0x0000000005080008UL, 0x000000000a110011UL, 0x0000000014220022UL,
            0x0000000028440044UL, 0x0000000050880088UL, 0x00000000a0100010UL, 0x0000000040200020UL,
            0x0000000204000402UL, 0x0000000508000805UL, 0x0000000a1100110aUL, 0x0000001422002214UL,
            0x0000002844004428UL, 0x0000005088008850UL, 0x000000a0100010a0UL, 0x0000004020002040UL,
            0x0000020400040200UL, 0x0000050800080500UL, 0x00000a1100110a00UL, 0x0000142200221400UL,
            0x0000284400442800UL, 0x0000508800885000UL, 0x0000a0100010a000UL, 0x0000402000204000UL,
            0x0002040004020000UL, 0x0005080008050000UL, 0x000a1100110a0000UL, 0x0014220022140000UL,
            0x0028440044280000UL, 0x0050880088500000UL, 0x00a0100010a00000UL, 0x0040200020400000UL,
            0x0204000402000000UL, 0x0508000805000000UL, 0x0a1100110a000000UL, 0x1422002214000000UL,
            0x2844004428000000UL, 0x5088008850000000UL, 0xa0100010a0000000UL, 0x4020002040000000UL,
            0x0400040200000000UL, 0x0800080500000000UL, 0x1100110a00000000UL, 0x2200221400000000UL,
            0x4400442800000000UL, 0x8800885000000000UL, 0x100010a000000000UL, 0x2000204000000000UL,
            0x0004020000000000UL, 0x0008050000000000UL, 0x00110a0000000000UL, 0x0022140000000000UL,
            0x0044280000000000UL, 0x0088500000000000UL, 0x0010a00000000000UL, 0x0020400000000000UL
        ];
        private readonly ulong[] kingPrecomputedAttacks = [
            0x0000000000000302UL, 0x0000000000000705UL, 0x0000000000000e0aUL, 0x0000000000001c14UL,
            0x0000000000003828UL, 0x0000000000007050UL, 0x000000000000e0a0UL, 0x000000000000c040UL,
            0x0000000000030203UL, 0x0000000000070507UL, 0x00000000000e0a0eUL, 0x00000000001c141cUL,
            0x0000000000382838UL, 0x0000000000705070UL, 0x0000000000e0a0e0UL, 0x0000000000c040c0UL,
            0x0000000003020300UL, 0x0000000007050700UL, 0x000000000e0a0e00UL, 0x000000001c141c00UL,
            0x0000000038283800UL, 0x0000000070507000UL, 0x00000000e0a0e000UL, 0x00000000c040c000UL,
            0x0000000302030000UL, 0x0000000705070000UL, 0x0000000e0a0e0000UL, 0x0000001c141c0000UL,
            0x0000003828380000UL, 0x0000007050700000UL, 0x000000e0a0e00000UL, 0x000000c040c00000UL,
            0x0000030203000000UL, 0x0000070507000000UL, 0x00000e0a0e000000UL, 0x00001c141c000000UL,
            0x0000382838000000UL, 0x0000705070000000UL, 0x0000e0a0e0000000UL, 0x0000c040c0000000UL,
            0x0003020300000000UL, 0x0007050700000000UL, 0x000e0a0e00000000UL, 0x001c141c00000000UL,
            0x0038283800000000UL, 0x0070507000000000UL, 0x00e0a0e000000000UL, 0x00c040c000000000UL,
            0x0302030000000000UL, 0x0705070000000000UL, 0x0e0a0e0000000000UL, 0x1c141c0000000000UL,
            0x3828380000000000UL, 0x7050700000000000UL, 0xe0a0e00000000000UL, 0xc040c00000000000UL,
            0x0203000000000000UL, 0x0507000000000000UL, 0x0a0e000000000000UL, 0x141c000000000000UL,
            0x2838000000000000UL, 0x5070000000000000UL, 0xa0e0000000000000UL, 0x40c0000000000000UL
        ];
        private readonly ulong[] precomputedRays = [
            0x8040201008040302UL, 0x02824222120a0705UL, 0x0404844424150e0aUL, 0x08080888492a1c14UL, 
            0x1010101192543828UL, 0x2020212224a87050UL, 0x404142444850e0a0UL, 0x010204081020c040UL, 
            0x402010080403fe03UL, 0x824222120a07fd07UL, 0x04844424150efb0eUL, 0x080888492a1cf71cUL,
            0x101011925438ef38UL, 0x20212224a870df70UL, 0x4142444850e0bfe0UL, 0x0204081020c07fc0UL,
            0x2010080403fe0304UL, 0x4222120a07fd070aUL, 0x844424150efb0e15UL, 0x0888492a1cf71c2aUL,
            0x1011925438ef3854UL, 0x212224a870df70a8UL, 0x42444850e0bfe050UL, 0x04081020c07fc020UL,
            0x10080403fe030408UL, 0x22120a07fd070a12UL, 0x4424150efb0e1524UL, 0x88492a1cf71c2a49UL,
            0x11925438ef385492UL, 0x2224a870df70a824UL, 0x444850e0bfe05048UL, 0x081020c07fc02010UL,
            0x080403fe03040810UL, 0x120a07fd070a1222UL, 0x24150efb0e152444UL, 0x492a1cf71c2a4988UL,
            0x925438ef38549211UL, 0x24a870df70a82422UL, 0x4850e0bfe0504844UL, 0x1020c07fc0201008UL,
            0x0403fe0304081020UL, 0x0a07fd070a122242UL, 0x150efb0e15244484UL, 0x2a1cf71c2a498808UL,
            0x5438ef3854921110UL, 0xa870df70a8242221UL, 0x50e0bfe050484442UL, 0x20c07fc020100804UL,
            0x03fe030408102040UL, 0x07fd070a12224282UL, 0x0efb0e1524448404UL, 0x1cf71c2a49880808UL,
            0x38ef385492111010UL, 0x70df70a824222120UL, 0xe0bfe05048444241UL, 0xc07fc02010080402UL,
            0x0203040810204080UL, 0x05070a1222428202UL, 0x0a0e152444840404UL, 0x141c2a4988080808UL,
            0x2838549211101010UL, 0x5070a82422212020UL, 0xa0e0504844424140UL, 0x40c0201008040201UL
        ];
        // Constructor and Singleton Instance
        private Magics()
        {
            Init();
        }
        public static Magics Instance()
        {
            if (instance == null)
            {
                lock (lockObj)
                {
                    instance ??= new Magics();
                }
            }
            return instance;
        }
        public void Init()
        {
            int offset = 0;

            // Initialize bishop magics
            for (int square = 0; square < 64; square++)
            {
                bishopMagic[square].mask = BishopAttackMask(square);
                bishopMagic[square].magic = bishopMagicNumbers[square];
                bishopMagic[square].shift = 64 - BitOperations.PopCount(bishopMagic[square].mask);
                bishopMagic[square].tableOffset = offset;

                // Calculate space needed for this square's attacks
                int size = 1 << BitOperations.PopCount(bishopMagic[square].mask);

                // Generate all possible blocker configurations
                List<ulong> blockerPatterns = GenerateBlockerPatterns(bishopMagic[square].mask);

                // Fill this section of the attack table
                foreach (ulong blockers in blockerPatterns)
                {
                    // Calculate the index into the attack table using the magic
                    ulong index = (blockers * bishopMagic[square].magic) >> bishopMagic[square].shift;

                    // Calculate the attacks for this blocker configuration
                    ulong attacks = CalculateBishopAttacks(square, blockers);

                    // Store the attacks in the table
                    attackTable[bishopMagic[square].tableOffset + (int)index] = attacks;
                }

                // Advance offset for next square
                offset += size;
            }

            // Initialize rook magics
            for (int square = 0; square < 64; square++)
            {
                // Similar implementation for rooks
                rookMagic[square].mask = RookAttackMask(square);
                rookMagic[square].magic = rookMagicNumbers[square];
                rookMagic[square].shift = 64 - BitOperations.PopCount(rookMagic[square].mask);
                rookMagic[square].tableOffset = offset;

                int size = 1 << BitOperations.PopCount(rookMagic[square].mask);
                List<ulong> blockerPatterns = GenerateBlockerPatterns(rookMagic[square].mask);

                foreach (ulong blockers in blockerPatterns)
                {
                    ulong index = (blockers * rookMagic[square].magic) >> rookMagic[square].shift;
                    ulong attacks = CalculateRookAttacks(square, blockers);
                    attackTable[rookMagic[square].tableOffset + (int)index] = attacks;
                }

                offset += size;
            }
        }

        // Methods to get attacks for pieces
        public ulong GetBishopAttacks(int square, ulong occupancy)
        {
            occupancy &= bishopMagic[square].mask;
            occupancy *= bishopMagic[square].magic;
            occupancy >>= bishopMagic[square].shift;
            return attackTable[bishopMagic[square].tableOffset + (int)occupancy];
        }
        public ulong GetRookAttacks(int square, ulong occupancy)
        {
            occupancy &= rookMagic[square].mask;
            occupancy *= rookMagic[square].magic;
            occupancy >>= rookMagic[square].shift;
            return attackTable[rookMagic[square].tableOffset + (int)occupancy];
        }
        public ulong GetQueenAttacks(int square, ulong occupancy)
        {
            return GetBishopAttacks(square, occupancy) | GetRookAttacks(square, occupancy);
        }
        public ulong GetKnightMoves(int square)
        {
            return knightPrecomputedAttacks[square];
        }
        public ulong GetKingMoves(int square)
        {
            return kingPrecomputedAttacks[square];
        }

        // Simple rays for pins and other stuff (not occupancy dependent)
        public ulong GetRay(int square)
        {
            return precomputedRays[square];
        }

        // Attack masks for bishops and rooks
        private static ulong BishopAttackMask(int square)
        {
            ulong attacks = 0;
            int targetRank = square / 8;
            int targetFile = square % 8;
            int rank, file;

            for (rank = targetRank + 1, file = targetFile + 1; rank <= 6 && file <= 6; rank++, file++) attacks |= 1ul << (rank * 8 + file);
            for (rank = targetRank - 1, file = targetFile - 1; rank >= 1 && file >= 1; rank--, file--) attacks |= 1ul << (rank * 8 + file);
            for (rank = targetRank + 1, file = targetFile - 1; rank <= 6 && file >= 1; rank++, file--) attacks |= 1ul << (rank * 8 + file);
            for (rank = targetRank - 1, file = targetFile + 1; rank >= 1 && file <= 6; rank--, file++) attacks |= 1ul << (rank * 8 + file);

            return attacks;
        }
        private static ulong RookAttackMask(int square)
        {
            ulong attacks = 0;
            int targetRank = square / 8;
            int targetFile = square % 8;
            int rank, file;

            for (rank = targetRank + 1; rank <= 6; rank++) attacks |= 1ul << (rank * 8 + targetFile);
            for (rank = targetRank - 1; rank >= 1; rank--) attacks |= 1ul << (rank * 8 + targetFile);
            for (file = targetFile + 1; file <= 6; file++) attacks |= 1ul << (targetRank * 8 + file);
            for (file = targetFile - 1; file >= 1; file--) attacks |= 1ul << (targetRank * 8 + file);

            return attacks;
        }

        // Generate all possible blocker patterns from a mask
        private static List<ulong> GenerateBlockerPatterns(ulong mask)
        {
            // Get positions of all bits in the mask
            List<int> bits = new List<int>();
            for (int i = 0; i < 64; i++)
                if (((mask >> i) & 1) == 1)
                    bits.Add(i);

            int count = bits.Count;
            int patterns = 1 << count;
            List<ulong> result = new(patterns);

            // Generate each pattern
            for (int i = 0; i < patterns; i++)
            {
                ulong blockers = 0;
                for (int j = 0; j < count; j++)
                    if (((i >> j) & 1) == 1)
                        blockers |= 1UL << bits[j];

                result.Add(blockers);
            }

            return result;
        }

        // Calculate bishop attacks with blockers
        private static ulong CalculateBishopAttacks(int square, ulong blockers)
        {
            ulong attacks = 0;
            int targetRank = square / 8;
            int targetFile = square % 8;
            int file, rank;

            // Northeast
            for (rank = targetRank + 1, file = targetFile + 1; rank <= 7 && file <= 7; rank++, file++)
            {
                attacks |= 1ul << (rank * 8 + file);
                if ((blockers & (1ul << (rank * 8 + file))) != 0) break;
            }

            // Southeast
            for (rank = targetRank - 1, file = targetFile + 1; rank >= 0 && file <= 7; rank--, file++)
            {
                attacks |= 1ul << (rank * 8 + file);
                if ((blockers & (1ul << (rank * 8 + file))) != 0) break;
            }

            // Southwest
            for (rank = targetRank - 1, file = targetFile - 1; rank >= 0 && file >= 0; rank--, file--)
            {
                attacks |= 1ul << (rank * 8 + file);
                if ((blockers & (1ul << (rank * 8 + file))) != 0) break;
            }

            // Northwest
            for (rank = targetRank + 1, file = targetFile - 1; rank <= 7 && file >= 0; rank++, file--)
            {
                attacks |= 1ul << (rank * 8 + file);
                if ((blockers & (1ul << (rank * 8 + file))) != 0) break;
            }

            return attacks;
        }

        // Calculate rook attacks with blockers
        private static ulong CalculateRookAttacks(int square, ulong blockers)
        {
            ulong attacks = 0;
            int targetRank = square / 8;
            int targetFile = square % 8;
            int file, rank;

            // North
            for (rank = targetRank + 1; rank <= 7; rank++)
            {
                attacks |= 1ul << (rank * 8 + targetFile);
                if ((blockers & (1ul << (rank * 8 + targetFile))) != 0) break;
            }
            // South
            for (rank = targetRank - 1; rank >= 0; rank--)
            {
                attacks |= 1ul << (rank * 8 + targetFile);
                if ((blockers & (1ul << (rank * 8 + targetFile))) != 0) break;
            }
            // East
            for (file = targetFile + 1; file <= 7; file++)
            {
                attacks |= 1ul << (targetRank * 8 + file);
                if ((blockers & (1ul << (targetRank * 8 + file))) != 0) break;
            }
            // West
            for (file = targetFile - 1; file >= 0; file--)
            {
                attacks |= 1ul << (targetRank * 8 + file);
                if ((blockers & (1ul << (targetRank * 8 + file))) != 0) break;
            }

            return attacks;
        }

    }
}