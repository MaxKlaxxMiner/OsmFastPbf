using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OsmFastPbf.zlibTuned.FastInflater
{
  /// <summary>
  /// Klasse zum Dekomprimieren eines Huffman-Baumes
  /// </summary>
  public sealed class InflaterHuffmanTree
  {
    static readonly int MAX_BITLEN = 15;
    short[] tree;
    public static readonly InflaterHuffmanTree defLitLenTree;
    public static readonly InflaterHuffmanTree defDistTree;

    static InflaterHuffmanTree()
    {
      try
      {
        var codeLengths = new byte[288];
        int i = 0;
        while (i < 144) codeLengths[i++] = 8;
        while (i < 256) codeLengths[i++] = 9;
        while (i < 280) codeLengths[i++] = 7;
        while (i < 288) codeLengths[i++] = 8;
        defLitLenTree = new InflaterHuffmanTree(codeLengths);
        codeLengths = new byte[32];
        i = 0;
        while (i < 32) codeLengths[i++] = 5;
        defDistTree = new InflaterHuffmanTree(codeLengths);
      }
      catch (Exception)
      {
        throw new ApplicationException("InflaterHuffmanTree: static tree length illegal");
      }
    }
    public InflaterHuffmanTree(byte[] codeLengths)
    {
      BuildTree(codeLengths);
    }
    static unsafe byte* GetUnsafe(byte[] buf)
    {
      var p = (byte*)Marshal.AllocHGlobal(buf.Length + 64);
      while (((ulong)p & 63) != 0) p++;
      for (int i = 0; i < buf.Length; i++) p[i] = buf[i];
      return p;
    }
    static readonly unsafe byte* bit8Reverse = GetUnsafe(Enumerable.Range(0, 256).Select(x => (byte)((((byte)(uint)x * 0x80200802UL) & 0x0884422110UL) * 0x0101010101UL >> 32)).ToArray());
    static unsafe short BitReverse(int value)
    {
      return (short)(ushort)((uint)bit8Reverse[value & 0xff] << 8 | bit8Reverse[(value >> 8) & 0xff]);
    }
    //static short BitReverse(int v)
    //{
    //  v = ((v >> 1) & 0x5555) | ((v & 0x5555) << 1);
    //  v = ((v >> 2) & 0x3333) | ((v & 0x3333) << 2);
    //  v = ((v >> 4) & 0x0F0F) | ((v & 0x0F0F) << 4);
    //  v = ((v >> 8) & 0x00FF) | ((v & 0x00FF) << 8);
    //  return (short)(ushort)v;
    //}
    void BuildTree(byte[] codeLengths)
    {
      var blCount = new int[MAX_BITLEN + 1];
      var nextCode = new int[MAX_BITLEN + 1];
      for (int i = 0; i < codeLengths.Length; i++)
      {
        int bits = codeLengths[i];
        if (bits > 0) blCount[bits]++;
      }
      int code = 0;
      int treeSize = 512;
      for (int bits = 1; bits <= MAX_BITLEN; bits++)
      {
        nextCode[bits] = code;
        code += blCount[bits] << (16 - bits);
        if (bits >= 10)
        {
          int start = nextCode[bits] & 0x1ff80;
          int end = code & 0x1ff80;
          treeSize += (end - start) >> (16 - bits);
        }
      }
      tree = new short[treeSize];
      int treePtr = 512;
      for (int bits = MAX_BITLEN; bits >= 10; bits--)
      {
        int end = code & 0x1ff80;
        code -= blCount[bits] << (16 - bits);
        int start = code & 0x1ff80;
        for (int i = start; i < end; i += 1 << 7)
        {
          tree[BitReverse(i)] = (short)((-treePtr << 4) | bits);
          treePtr += 1 << (bits - 9);
        }
      }
      for (int i = 0; i < codeLengths.Length; i++)
      {
        int bits = codeLengths[i];
        if (bits == 0) continue;
        code = nextCode[bits];
        int revcode = BitReverse(code);
        if (bits <= 9)
        {
          do
          {
            tree[revcode] = (short)((i << 4) | bits);
            revcode += 1 << bits;
          } while (revcode < 512);
        }
        else
        {
          int subTree = tree[revcode & 511];
          int treeLen = 1 << (subTree & 15);
          subTree = -(subTree >> 4);
          do
          {
            tree[subTree | (revcode >> 9)] = (short)((i << 4) | bits);
            revcode += 1 << bits;
          } while (revcode < treeLen);
        }
        nextCode[bits] = code + (1 << (16 - bits));
      }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSymbol(StreamManipulator input)
    {
      int lookahead, symbol;
      if ((lookahead = input.PeekBits(9)) >= 0)
      {
        if ((symbol = tree[lookahead]) >= 0)
        {
          input.DropBits(symbol & 15);
          return symbol >> 4;
        }
        int subtree = -(symbol >> 4);
        int bitlen = symbol & 15;
        if ((lookahead = input.PeekBits(bitlen)) >= 0)
        {
          symbol = tree[subtree | (lookahead >> 9)];
          input.DropBits(symbol & 15);
          return symbol >> 4;
        }
        int bits = input.AvailableBits;
        lookahead = input.PeekBits(bits);
        symbol = tree[subtree | (lookahead >> 9)];
        if ((symbol & 15) <= bits)
        {
          input.DropBits(symbol & 15);
          return symbol >> 4;
        }
        return -1;
      }
      else
      {
        int bits = input.AvailableBits;
        lookahead = input.PeekBits(bits);
        symbol = tree[lookahead];
        if (symbol >= 0 && (symbol & 15) <= bits)
        {
          input.DropBits(symbol & 15);
          return symbol >> 4;
        }
        return -1;
      }
    }
  }
}
