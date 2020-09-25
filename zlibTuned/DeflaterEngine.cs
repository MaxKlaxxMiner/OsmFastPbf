
#region # using *.*

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

#endregion

namespace OsmFastPbf.zlibTuned
{
  /// <summary>
  /// Verarbeitungsklasse für die Deflate-Komprimierung
  /// </summary>
  sealed unsafe class DeflaterEngine
  {
    private readonly int TooFar;
    int insH;
    const int UnsafeBytes = DeflaterConstants.Wsize * 4 + DeflaterConstants.HashSize * 2;
    const int MemPrev = DeflaterConstants.Wsize;
    const int MemHead = MemPrev + DeflaterConstants.Wsize;
    public readonly byte[] memFix = new byte[UnsafeBytes];
    int matchStart, matchLen;
    bool prevAvailable;
    int blockStart;
    int strstart;
    int lookahead;
    readonly int maxChain;
    readonly int maxLazy;
    readonly int niceLength;
    readonly int goodLength;
    readonly int comprFunc;
    byte[] inputBuf;
    int totalIn;
    int inputOff;
    int inputEnd;
    readonly PendingBuffer pending;
    readonly DeflaterHuffman huffman;

    public DeflaterEngine(PendingBuffer pending, int lvl, int tooFar = 4096)
    {
      this.pending = pending;
      huffman = new DeflaterHuffman(pending);
      blockStart = strstart = 1;
      goodLength = DeflaterConstants.GoodLength[lvl];
      maxLazy = DeflaterConstants.MaxLazy[lvl];
      niceLength = DeflaterConstants.NiceLength[lvl];
      maxChain = DeflaterConstants.MaxChain[lvl];
      comprFunc = DeflaterConstants.ComprFunc[lvl];
      insH = 0;
      matchStart = 0;
      matchLen = 0;
      prevAvailable = false;
      lookahead = 0;
      totalIn = 0;
      inputBuf = null;
      inputOff = 0;
      inputEnd = 0;
      TooFar = tooFar;
    }

    public void Reset(byte* mem)
    {
      huffman.Reset();
      blockStart = strstart = 1;
      lookahead = 0;
      totalIn = 0;
      prevAvailable = false;
      matchLen = DeflaterConstants.MinMatch - 1;
      for (int i = 0; i < UnsafeBytes / 8; i++) *((long*)mem + i) = 0;
    }

    public int TotalIn
    {
      get
      {
        return totalIn;
      }
    }

    void UpdateHash(byte* mem)
    {
      insH = (mem[strstart] << DeflaterConstants.HashShift) ^ mem[strstart + 1];
    }

    int InsertString(byte* mem)
    {
      int hash = ((insH << DeflaterConstants.HashShift) ^ mem[strstart + (DeflaterConstants.MinMatch - 1)]) & DeflaterConstants.HashMask;
      ushort match = *((ushort*)mem + hash + MemHead);
      *((ushort*)mem + (strstart & DeflaterConstants.Wmask) + MemPrev) = match;
      *((ushort*)mem + hash + MemHead) = (ushort)strstart;
      insH = hash;
      return match;
    }

    static void SubSlide(ushort* data, int count, ushort* slideValues)
    {
      for (int n = 0; n < count; data += 8, n += 8)
      {
        var num1 = *(data + 0);
        var num2 = *(data + 1);
        var num3 = *(data + 2);
        var num4 = *(data + 3);
        var num5 = *(data + 4);
        var num6 = *(data + 5);
        var num7 = *(data + 6);
        var num8 = *(data + 7);
        num1 = slideValues[num1];
        num2 = slideValues[num2];
        num3 = slideValues[num3];
        num4 = slideValues[num4];
        num5 = slideValues[num5];
        num6 = slideValues[num6];
        num7 = slideValues[num7];
        num8 = slideValues[num8];
        *(data + 0) = num1;
        *(data + 1) = num2;
        *(data + 2) = num3;
        *(data + 3) = num4;
        *(data + 4) = num5;
        *(data + 5) = num6;
        *(data + 6) = num7;
        *(data + 7) = num8;
      }
    }

    static readonly ushort[] SlideValues = Enumerable.Range(0, 65536).Select(x => (ushort)(x < DeflaterConstants.Wsize ? 0 : x - DeflaterConstants.Wsize)).ToArray();

    static void Memmove(byte* dest, byte* src, int len)
    {
      for (int i = 0; i < len; i += 16)
      {
        *(long*)(dest + i) = *(long*)(src + i);
        *(long*)(dest + i + 8) = *(long*)(src + i + 8);
      }
    }

    void SlideWindow(byte* mem)
    {
      Memmove(mem, mem + DeflaterConstants.Wsize, DeflaterConstants.Wsize);
      matchStart -= DeflaterConstants.Wsize;
      strstart -= DeflaterConstants.Wsize;
      blockStart -= DeflaterConstants.Wsize;
      fixed (ushort* slideValues = SlideValues)
      {
        SubSlide((ushort*)mem + MemHead, DeflaterConstants.HashSize, slideValues);
        SubSlide((ushort*)mem + MemPrev, DeflaterConstants.Wsize, slideValues);
      }
    }

    void FillWindow(byte* mem)
    {
      if (strstart >= DeflaterConstants.Wsize + DeflaterConstants.MaxDist) SlideWindow(mem);
      while (lookahead < DeflaterConstants.MinLookahead && inputOff < inputEnd)
      {
        int more = 2 * DeflaterConstants.Wsize - lookahead - strstart;
        if (more > inputEnd - inputOff) more = inputEnd - inputOff;
        Marshal.Copy(inputBuf, inputOff, (IntPtr)(mem + strstart + lookahead), more);
        inputOff += more;
        totalIn += more;
        lookahead += more;
      }
      if (lookahead >= DeflaterConstants.MinMatch) UpdateHash(mem);
    }

    int FindLongestMatch(int curMatch, byte* mem, int prevLen)
    {
      int chainLength = maxChain;
      var strstartP = mem + strstart;
      int nLen = niceLength;
      var bestEnd = strstartP + prevLen;
      int bestLen = Math.Max(prevLen, DeflaterConstants.MinMatch - 1);
      int limit = Math.Max(strstart - DeflaterConstants.MaxDist, 0);
      var strend = strstartP + DeflaterConstants.MaxMatch - 1;
      if (bestLen >= goodLength) chainLength >>= 2;
      if (nLen > lookahead) nLen = lookahead;
      bestLen = FindBestLen(mem, curMatch, bestLen, strend, bestEnd, nLen, limit, chainLength, strstartP, ref matchStart);
      return Math.Min(bestLen, lookahead);
    }

    static int FindBestLen(byte* mem, int curMatch, int bestLen, byte* strend, byte* bestEnd, int nLen, int limit, int chainLength, byte* strstart, ref int matchStart)
    {
      short scanEnd = *(short*)(bestEnd - 1);
      do
      {
        if (*(short*)(mem + curMatch + bestLen - 1) == scanEnd && *(short*)(mem + curMatch) == *(short*)strstart)
        {
          var scan = ScanCompare(strstart + 2, mem + curMatch + 2, strend);
          if (scan > bestEnd)
          {
            matchStart = curMatch;
            bestEnd = scan;
            bestLen = (int)(scan - strstart);
            if (bestLen >= nLen) break;
            if (bestLen >= 8) return FindBestLen8(mem, curMatch, bestLen, strend, bestEnd, nLen, limit, chainLength, strstart, ref matchStart);
            scanEnd = *(short*)(bestEnd - 1);
          }
        }
        curMatch = *((ushort*)mem + (curMatch & DeflaterConstants.Wmask) + MemPrev);
      } while (curMatch > limit && --chainLength != 0);
      return bestLen;
    }

    static int FindBestLen8(byte* mem, int curMatch, int bestLen, byte* strend, byte* bestEnd, int nLen, int limit, int chainLength, byte* strstart, ref int matchStart)
    {
      long scanEnd = *(long*)(bestEnd - 7);
      do
      {
        if (*(long*)(mem + curMatch + bestLen - 7) == scanEnd && *(long*)(mem + curMatch) == *(long*)strstart)
        {
          var scan = ScanCompare(strstart + 8, mem + curMatch + 8, strend);
          if (scan > strend + 1) scan = strend + 1;
          if (scan > bestEnd && strstart[8] == mem[curMatch + 8])
          {
            matchStart = curMatch;
            bestEnd = scan;
            bestLen = (int)(scan - strstart);
            if (bestLen >= nLen) break;
            scanEnd = *(long*)(bestEnd - 7);
          }
        }
        curMatch = *((ushort*)mem + (curMatch & DeflaterConstants.Wmask) + MemPrev);
      } while (curMatch > limit && --chainLength != 0);
      return bestLen;
    }

    static byte* ScanCompare(byte* scan, byte* match, byte* strend)
    {
      do
      {
        if (*(long*)(scan + 1) == *(long*)(match + 1))
        {
          scan += 8;
          match += 8;
          continue;
        }
        if (*++scan != *++match ||
            *++scan != *++match ||
            *++scan != *++match ||
            *++scan != *++match ||
            *++scan != *++match ||
            *++scan != *++match ||
            *++scan != *++match ||
            *++scan != *++match) return scan;
      } while (scan < strend);
      return scan;
    }

    bool DoDeflateStored(bool flush, bool finish, byte* mem)
    {
      if (!flush && lookahead == 0) return false;
      strstart += lookahead;
      lookahead = 0;
      int storedLen = strstart - blockStart;
      if ((storedLen < DeflaterConstants.MaxBlockSize) && (blockStart >= DeflaterConstants.Wsize || storedLen < DeflaterConstants.MaxDist) && !flush) return true;
      bool lastBlock = finish;
      if (storedLen > DeflaterConstants.MaxBlockSize)
      {
        storedLen = DeflaterConstants.MaxBlockSize;
        lastBlock = false;
      }
      huffman.FlushStoredBlock(mem, blockStart, storedLen, lastBlock);
      blockStart += storedLen;
      return !lastBlock;
    }

    bool DoDeflateFast(bool flush, bool finish, byte* mem)
    {
      if (lookahead < DeflaterConstants.MinLookahead && !flush) return false;
      while (lookahead >= DeflaterConstants.MinLookahead || flush)
      {
        if (lookahead == 0)
        {
          huffman.FlushBlock(mem, blockStart, strstart - blockStart, finish);
          blockStart = strstart;
          return false;
        }
        if (strstart > 2 * DeflaterConstants.Wsize - DeflaterConstants.MinLookahead) SlideWindow(mem);
        int hashHead;
        if (lookahead >= DeflaterConstants.MinMatch && (hashHead = InsertString(mem)) != 0 && strstart - hashHead <= DeflaterConstants.MaxDist && (matchLen = FindLongestMatch(hashHead, mem, matchLen)) >= DeflaterConstants.MinMatch)
        {
          if (huffman.TallyDist(strstart - matchStart, matchLen))
          {
            bool lastBlock = finish && lookahead == 0;
            huffman.FlushBlock(mem, blockStart, strstart - blockStart, lastBlock);
            blockStart = strstart;
          }
          lookahead -= matchLen;
          if (matchLen <= maxLazy && lookahead >= DeflaterConstants.MinMatch)
          {
            while (--matchLen > 0)
            {
              ++strstart;
              InsertString(mem);
            }
            ++strstart;
          }
          else
          {
            strstart += matchLen;
            if (lookahead >= DeflaterConstants.MinMatch - 1) UpdateHash(mem);
          }
          matchLen = DeflaterConstants.MinMatch - 1;
          continue;
        }
        huffman.TallyLit(mem[strstart]);
        ++strstart;
        --lookahead;
        if (!huffman.IsFull()) continue;
        bool lastBlock2 = finish && lookahead == 0;
        huffman.FlushBlock(mem, blockStart, strstart - blockStart, lastBlock2);
        blockStart = strstart;
        return !lastBlock2;
      }
      return true;
    }

    bool DoDeflateSlow(bool flush, bool finish, byte* mem)
    {
      if (lookahead < DeflaterConstants.MinLookahead && !flush) return false;
      while (lookahead >= DeflaterConstants.MinLookahead || flush)
      {
        if (lookahead == 0)
        {
          if (prevAvailable) huffman.TallyLit(mem[strstart - 1]);
          prevAvailable = false;
          huffman.FlushBlock(mem, blockStart, strstart - blockStart, finish);
          blockStart = strstart;
          return false;
        }
        if (strstart >= 2 * DeflaterConstants.Wsize - DeflaterConstants.MinLookahead) SlideWindow(mem);
        int prevMatch = matchStart;
        int prevLen = matchLen;
        if (lookahead >= DeflaterConstants.MinMatch)
        {
          int hashHead = InsertString(mem);
          if (hashHead != 0 && strstart - hashHead <= DeflaterConstants.MaxDist && (matchLen = FindLongestMatch(hashHead, mem, matchLen)) >= DeflaterConstants.MinMatch)
          {
            if (matchLen <= 5 && matchLen == DeflaterConstants.MinMatch && strstart - matchStart > TooFar) matchLen = DeflaterConstants.MinMatch - 1;
          }
        }
        if (prevLen >= DeflaterConstants.MinMatch && matchLen <= prevLen)
        {
          huffman.TallyDist(strstart - 1 - prevMatch, prevLen);
          prevLen -= 2;
          do
          {
            strstart++;
            lookahead--;
            if (lookahead >= DeflaterConstants.MinMatch) InsertString(mem);
          } while (--prevLen > 0);
          strstart++;
          lookahead--;
          prevAvailable = false;
          matchLen = DeflaterConstants.MinMatch - 1;
        }
        else
        {
          if (prevAvailable) huffman.TallyLit(mem[strstart - 1]);
          prevAvailable = true;
          strstart++;
          lookahead--;
        }
        if (!huffman.IsFull()) continue;
        int len = strstart - blockStart;
        if (prevAvailable) len--;
        bool lastBlock = (finish && lookahead == 0 && !prevAvailable);
        huffman.FlushBlock(mem, blockStart, len, lastBlock);
        blockStart += len;
        return !lastBlock;
      }
      return true;
    }

    public bool Deflate(bool flush, bool finish, byte* mem)
    {
      bool progress;
      do
      {
        FillWindow(mem);
        bool canFlush = flush && inputOff == inputEnd;
        switch (comprFunc)
        {
          case DeflaterConstants.DeflateStored: progress = DoDeflateStored(canFlush, finish, mem); break;
          case DeflaterConstants.DeflateFast: progress = DoDeflateFast(canFlush, finish, mem); break;
          case DeflaterConstants.DeflateSlow: progress = DoDeflateSlow(canFlush, finish, mem); break;
          default: throw new InvalidOperationException("unknown comprFunc");
        }
      } while (pending.IsFlushed && progress);
      return progress;
    }

    public void SetInput(byte[] buf, int off, int len)
    {
      if (inputOff < inputEnd) throw new InvalidOperationException("Old input was not completely processed");
      int end = off + len;
      if (0 > off || off > end || end > buf.Length) throw new ArgumentOutOfRangeException();
      inputBuf = buf;
      inputOff = off;
      inputEnd = end;
    }

    public bool NeedsInput()
    {
      return inputEnd == inputOff;
    }
  }
}
