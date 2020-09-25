
#region # using *.*

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace OsmFastPbf.zlibTuned
{
  /// <summary>
  /// Verarbeitungsklasse zum komprimieren nach dem Huffman-Prinzip
  /// </summary>
  unsafe sealed class DeflaterHuffman
  {
    const int Bufsize = 1 << (DeflaterConstants.DefaultMemLevel + 6);
    const int LiteralNum = 286;
    const int DistNum = 30;
    const int BitlenNum = 19;
    const int Rep36 = 16;
    const int Rep310 = 17;
    const int Rep11138 = 18;
    const int EofSymbol = 256;
    static readonly int[] BlOrder = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
    static readonly byte[] Bit4Reverse = { 0, 8, 4, 12, 2, 10, 6, 14, 1, 9, 5, 13, 3, 11, 7, 15 };

    public sealed class Tree
    {
      public readonly short[] freqs;
      public byte[] length;
      readonly int minNumCodes;
      public int numCodes;
      ushort[] codes;
      readonly int[] blCounts;
      readonly int maxLength;
      readonly DeflaterHuffman dh;

      public Tree(DeflaterHuffman dh, int elems, int minCodes, int maxLength)
      {
        this.dh = dh;
        minNumCodes = minCodes;
        this.maxLength = maxLength;
        freqs = new short[elems];
        blCounts = new int[maxLength];
      }

      public void Reset()
      {
        for (int i = 0; i < freqs.Length; i++) freqs[i] = 0;
        codes = null;
        length = null;
      }

      public void WriteSymbol(int code)
      {
        dh.pending.WriteBits(codes[code], length[code]);
      }

      public void SetStaticCodes(ushort[] stCodes, byte[] stLength)
      {
        codes = stCodes;
        length = stLength;
      }

      public void BuildCodes()
      {
        var nextCode = new int[maxLength];
        int code = 0;
        codes = new ushort[freqs.Length];
        for (int bits = 0; bits < maxLength; bits++)
        {
          nextCode[bits] = code;
          code += blCounts[bits] << (15 - bits);
        }
        for (int i = 0; i < numCodes; i++)
        {
          int bits = length[i];
          if (bits <= 0) continue;
          codes[i] = BitReverse(nextCode[bits - 1]);
          nextCode[bits - 1] += 1 << (16 - bits);
        }
      }

      void BuildLength(int[] childs)
      {
        length = new byte[freqs.Length];
        int numNodes = childs.Length / 2;
        int numLeafs = (numNodes + 1) / 2;
        int overflow = 0;
        for (int i = 0; i < maxLength; i++) blCounts[i] = 0;
        var lengths = new int[numNodes];
        lengths[numNodes - 1] = 0;
        for (int i = numNodes - 1; i >= 0; i--)
        {
          if (childs[2 * i + 1] != -1)
          {
            int bitLength = lengths[i] + 1;
            if (bitLength > maxLength)
            {
              bitLength = maxLength;
              overflow++;
            }
            lengths[childs[2 * i]] = lengths[childs[2 * i + 1]] = bitLength;
          }
          else
          {
            int bitLength = lengths[i];
            blCounts[bitLength - 1]++;
            length[childs[2 * i]] = (byte)lengths[i];
          }
        }
        if (overflow == 0) return;
        int incrBitLen = maxLength - 1;
        do
        {
          while (blCounts[--incrBitLen] == 0)
          {
          }
          do
          {
            blCounts[incrBitLen]--;
            blCounts[++incrBitLen]++;
            overflow -= 1 << (maxLength - 1 - incrBitLen);
          } while (overflow > 0 && incrBitLen < maxLength - 1);
        } while (overflow > 0);
        blCounts[maxLength - 1] += overflow;
        blCounts[maxLength - 2] -= overflow;
        int nodePtr = 2 * numLeafs;
        for (int bits = maxLength; bits != 0; bits--)
        {
          int n = blCounts[bits - 1];
          while (n > 0)
          {
            int childPtr = 2 * childs[nodePtr++];
            if (childs[childPtr + 1] != -1) continue;
            length[childs[childPtr]] = (byte)bits;
            n--;
          }
        }
      }

      public void BuildTree()
      {
        int numSymbols = freqs.Length;
        var heap = new int[numSymbols];
        int heapLen = 0;
        int maxCode = 0;
        for (int n = 0; n < numSymbols; n++)
        {
          int freq = freqs[n];
          if (freq == 0) continue;
          int pos = heapLen++;
          int ppos;
          while (pos > 0 && freqs[heap[ppos = (pos - 1) / 2]] > freq)
          {
            heap[pos] = heap[ppos];
            pos = ppos;
          }
          heap[pos] = n;
          maxCode = n;
        }
        while (heapLen < 2)
        {
          int node = maxCode < 2 ? ++maxCode : 0;
          heap[heapLen++] = node;
        }
        numCodes = Math.Max(maxCode + 1, minNumCodes);
        int numLeafs = heapLen;
        var childs = new int[4 * heapLen - 2];
        var values = new int[2 * heapLen - 1];
        int numNodes = numLeafs;
        for (int i = 0; i < heapLen; i++)
        {
          int node = heap[i];
          childs[2 * i] = node;
          childs[2 * i + 1] = -1;
          values[i] = freqs[node] << 8;
          heap[i] = i;
        }
        do
        {
          int first = heap[0];
          int last = heap[--heapLen];
          int ppos = 0;
          int path = 1;
          while (path < heapLen)
          {
            if (path + 1 < heapLen && values[heap[path]] > values[heap[path + 1]]) path++;
            heap[ppos] = heap[path];
            ppos = path;
            path = path * 2 + 1;
          }
          int lastVal = values[last];
          while ((path = ppos) > 0 && values[heap[ppos = (path - 1) / 2]] > lastVal) heap[path] = heap[ppos];
          heap[path] = last;
          int second = heap[0];
          last = numNodes++;
          childs[2 * last] = first;
          childs[2 * last + 1] = second;
          int mindepth = Math.Min(values[first] & 0xff, values[second] & 0xff);
          values[last] = lastVal = values[first] + values[second] - mindepth + 1;
          ppos = 0;
          path = 1;
          while (path < heapLen)
          {
            if (path + 1 < heapLen && values[heap[path]] > values[heap[path + 1]]) path++;
            heap[ppos] = heap[path];
            ppos = path;
            path = ppos * 2 + 1;
          }
          while ((path = ppos) > 0 && values[heap[ppos = (path - 1) / 2]] > lastVal) heap[path] = heap[ppos];
          heap[path] = last;
        } while (heapLen > 1);
        if (heap[0] != childs.Length / 2 - 1) throw new Exception("Weird!");
        BuildLength(childs);
      }

      public int GetEncodedLength()
      {
        int len = 0;
        for (int i = 0; i < freqs.Length; i++) len += freqs[i] * length[i];
        return len;
      }

      public void CalcBlFreq(Tree blTree)
      {
        int curlen = -1;
        int i = 0;
        while (i < numCodes)
        {
          var count = 1;
          int nextlen = length[i];
          int maxCount;
          int minCount;
          if (nextlen == 0)
          {
            maxCount = 138;
            minCount = 3;
          }
          else
          {
            maxCount = 6;
            minCount = 3;
            if (curlen != nextlen)
            {
              blTree.freqs[nextlen]++;
              count = 0;
            }
          }
          curlen = nextlen;
          i++;
          while (i < numCodes && curlen == length[i])
          {
            i++;
            if (++count >= maxCount) break;
          }
          if (count < minCount) blTree.freqs[curlen] += (short)count;
          else if (curlen != 0) blTree.freqs[Rep36]++;
          else if (count <= 10) blTree.freqs[Rep310]++;
          else blTree.freqs[Rep11138]++;
        }
      }

      public void WriteTree(Tree blTree)
      {
        int curlen = -1;
        int i = 0;
        while (i < numCodes)
        {
          var count = 1;
          int nextlen = length[i];
          int maxCount;
          int minCount;
          if (nextlen == 0)
          {
            maxCount = 138;
            minCount = 3;
          }
          else
          {
            maxCount = 6;
            minCount = 3;
            if (curlen != nextlen)
            {
              blTree.WriteSymbol(nextlen);
              count = 0;
            }
          }
          curlen = nextlen;
          i++;
          while (i < numCodes && curlen == length[i])
          {
            i++;
            if (++count >= maxCount) break;
          }
          if (count < minCount)
          {
            while (count-- > 0) blTree.WriteSymbol(curlen);
          }
          else if (curlen != 0)
          {
            blTree.WriteSymbol(Rep36);
            dh.pending.WriteBits(count - 3, 2);
          }
          else if (count <= 10)
          {
            blTree.WriteSymbol(Rep310);
            dh.pending.WriteBits(count - 3, 3);
          }
          else
          {
            blTree.WriteSymbol(Rep11138);
            dh.pending.WriteBits(count - 11, 7);
          }
        }
      }
    }

    readonly PendingBuffer pending;
    readonly Tree literalTree;
    readonly Tree distTree;
    readonly Tree blTree;
    readonly short[] dBuf;
    readonly byte[] lBuf;
    int lastLit;
    int extraBits;
    static readonly ushort[] StaticLCodes;
    static readonly byte[] StaticLLength;
    static readonly ushort[] StaticDCodes;
    static readonly byte[] StaticDLength;

    static ushort BitReverse(int value)
    {
      return (ushort)(Bit4Reverse[value & 0xF] << 12 |
                      Bit4Reverse[(value >> 4) & 0xF] << 8 |
                      Bit4Reverse[(value >> 8) & 0xF] << 4 |
                      Bit4Reverse[value >> 12]);
    }

    static DeflaterHuffman()
    {
      StaticLCodes = new ushort[LiteralNum];
      StaticLLength = new byte[LiteralNum];
      int i = 0;
      while (i < 144)
      {
        StaticLCodes[i] = BitReverse((0x030 + i) << 8);
        StaticLLength[i++] = 8;
      }
      while (i < 256)
      {
        StaticLCodes[i] = BitReverse((0x190 - 144 + i) << 7);
        StaticLLength[i++] = 9;
      }
      while (i < 280)
      {
        StaticLCodes[i] = BitReverse((0x000 - 256 + i) << 9);
        StaticLLength[i++] = 7;
      }
      while (i < LiteralNum)
      {
        StaticLCodes[i] = BitReverse((0x0c0 - 280 + i) << 8);
        StaticLLength[i++] = 8;
      }
      StaticDCodes = new ushort[DistNum];
      StaticDLength = new byte[DistNum];
      for (i = 0; i < DistNum; i++)
      {
        StaticDCodes[i] = BitReverse(i << 11);
        StaticDLength[i] = 5;
      }
    }

    public DeflaterHuffman(PendingBuffer pending)
    {
      this.pending = pending;
      literalTree = new Tree(this, LiteralNum, 257, 15);
      distTree = new Tree(this, DistNum, 1, 15);
      blTree = new Tree(this, BitlenNum, 4, 7);
      dBuf = new short[Bufsize];
      lBuf = new byte[Bufsize];
    }

    public void Reset()
    {
      lastLit = 0;
      extraBits = 0;
      literalTree.Reset();
      distTree.Reset();
      blTree.Reset();
    }

    static readonly int[] LcodeStatic = Enumerable.Range(0, 256).Select(Lcode).ToArray();

    static int Lcode(int len)
    {
      if (len == 255) return 285;
      int code = 257;
      while (len >= 8)
      {
        code += 4;
        len >>= 1;
      }
      return code + len;
    }

    static readonly int[] DcodeStatic = Enumerable.Range(0, 32768).Select(Dcode).ToArray();

    static int Dcode(int distance)
    {
      int code = 0;
      while (distance >= 4)
      {
        code += 2;
        distance >>= 1;
      }
      return code + distance;
    }

    void SendAllTrees(int blTreeCodes)
    {
      blTree.BuildCodes();
      literalTree.BuildCodes();
      distTree.BuildCodes();
      pending.WriteBits(literalTree.numCodes - 257, 5);
      pending.WriteBits(distTree.numCodes - 1, 5);
      pending.WriteBits(blTreeCodes - 4, 4);
      for (int rank = 0; rank < blTreeCodes; rank++)
      {
        pending.WriteBits(blTree.length[BlOrder[rank]], 3);
      }
      literalTree.WriteTree(blTree);
      distTree.WriteTree(blTree);
    }

    void CompressBlock()
    {
      for (int i = 0; i < lastLit; i++)
      {
        int litlen = lBuf[i];
        int dist = dBuf[i];
        if (dist-- != 0)
        {
          int lc = LcodeStatic[litlen];
          literalTree.WriteSymbol(lc);
          int bits = (lc - 261) / 4;
          if (bits > 0 && bits <= 5) pending.WriteBits(litlen & ((1 << bits) - 1), bits);
          int dc = DcodeStatic[dist];
          distTree.WriteSymbol(dc);
          bits = dc / 2 - 1;
          if (bits > 0) pending.WriteBits(dist & ((1 << bits) - 1), bits);
        }
        else literalTree.WriteSymbol(litlen);
      }
      literalTree.WriteSymbol(EofSymbol);
    }

    public void FlushStoredBlock(byte* stored, int storedOffset, int storedLength, bool lastBlock)
    {
      pending.WriteBits((DeflaterConstants.StoredBlock << 1) + (lastBlock ? 1 : 0), 3);
      pending.AlignToByte();
      pending.WriteShort(storedLength);
      pending.WriteShort(~storedLength);
      pending.WriteBlock(stored, storedOffset, storedLength);
      Reset();
    }

    public void FlushBlock(byte* stored, int storedOffset, int storedLength, bool lastBlock)
    {
      literalTree.freqs[EofSymbol]++;
      literalTree.BuildTree();
      distTree.BuildTree();
      literalTree.CalcBlFreq(blTree);
      distTree.CalcBlFreq(blTree);
      blTree.BuildTree();
      int blTreeCodes = 4;
      for (int i = 18; i > blTreeCodes; i--) if (blTree.length[BlOrder[i]] > 0) blTreeCodes = i + 1;
      int optLen = 14 + blTreeCodes * 3 + blTree.GetEncodedLength() + literalTree.GetEncodedLength() + distTree.GetEncodedLength() + extraBits;
      int staticLen = extraBits;
      for (int i = 0; i < LiteralNum; i++) staticLen += literalTree.freqs[i] * StaticLLength[i];
      for (int i = 0; i < DistNum; i++) staticLen += distTree.freqs[i] * StaticDLength[i];
      if (optLen >= staticLen) optLen = staticLen;
      if (storedOffset >= 0 && storedLength + 4 < optLen >> 3)
      {
        FlushStoredBlock(stored, storedOffset, storedLength, lastBlock);
      }
      else if (optLen == staticLen)
      {
        pending.WriteBits((DeflaterConstants.StaticTrees << 1) + (lastBlock ? 1 : 0), 3);
        literalTree.SetStaticCodes(StaticLCodes, StaticLLength);
        distTree.SetStaticCodes(StaticDCodes, StaticDLength);
        CompressBlock();
        Reset();
      }
      else
      {
        pending.WriteBits((DeflaterConstants.DynTrees << 1) + (lastBlock ? 1 : 0), 3);
        SendAllTrees(blTreeCodes);
        CompressBlock();
        Reset();
      }
    }

    public bool IsFull()
    {
      return lastLit >= Bufsize;
    }

    public void TallyLit(byte lit)
    {
      dBuf[lastLit] = 0;
      lBuf[lastLit++] = lit;
      literalTree.freqs[lit]++;
    }

    public bool TallyDist(int dist, int len)
    {
      dBuf[lastLit] = (short)dist;
      lBuf[lastLit++] = (byte)(len - 3);
      int lc = LcodeStatic[len - 3];
      literalTree.freqs[lc]++;
      if (lc >= 265 && lc < 285) extraBits += (lc - 261) / 4;
      int dc = DcodeStatic[dist - 1];
      distTree.freqs[dc]++;
      if (dc >= 4) extraBits += dc / 2 - 1;
      return IsFull();
    }
  }
}
