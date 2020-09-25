
namespace OsmFastPbf.zlibTuned
{
  /// <summary>
  /// Konstanten für den Deflater
  /// </summary>
  static class DeflaterConstants
  {
    public const int StoredBlock = 0;
    public const int StaticTrees = 1;
    public const int DynTrees = 2;
    public const int DefaultMemLevel = 8;
    public const int MaxMatch = 258;
    public const int MinMatch = 3;
    public const int MaxWbits = 15;
    public const int Wsize = 1 << MaxWbits;
    public const int Wmask = Wsize - 1;
    const int HashBits = DefaultMemLevel + 7;
    public const int HashSize = 1 << HashBits;
    public const int HashMask = HashSize - 1;
    public const int HashShift = (HashBits + MinMatch - 1) / MinMatch;
    public const int MinLookahead = MaxMatch + MinMatch + 1;
    public const int MaxDist = Wsize - MinLookahead;
    public const int PendingBufSize = 1 << (DefaultMemLevel + 8);
    public const int MaxBlockSize = PendingBufSize - 5;
    public const int DeflateStored = 0;
    public const int DeflateFast = 1;
    public const int DeflateSlow = 2;
    public static readonly int[] GoodLength = { 0, 4, 4, 4, 4, 8, 8, 8, 32, 32, 256 };
    public static readonly int[] MaxLazy = { 0, 4, 5, 6, 4, 16, 16, 32, 128, 258, 258 };
    public static readonly int[] NiceLength = { 0, 8, 16, 32, 16, 32, 128, 128, 258, 258, 258 };
    public static readonly int[] MaxChain = { 0, 4, 8, 32, 16, 32, 128, 256, 1024, 4096, 65536 };
    public static readonly int[] ComprFunc = { 0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2 };
  }
}
