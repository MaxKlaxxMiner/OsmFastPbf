using System;

namespace OsmFastPbf.zlibTuned.FastInflater
{
  /// <summary>
  /// Verarbeitungsklasse zum dekomprimieren von Deflate-Daten (wird z.B. von GnuZip und Zip verwendet)
  /// </summary>
  public sealed class Inflater
  {
    static readonly int[] CPLENS = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
    static readonly int[] CPLEXT = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
    static readonly int[] CPDIST = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
    static readonly int[] CPDEXT = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
    const int DECODE_BLOCKS = 2;
    const int DECODE_STORED_LEN1 = 3;
    const int DECODE_STORED_LEN2 = 4;
    const int DECODE_STORED = 5;
    const int DECODE_DYN_HEADER = 6;
    const int DECODE_HUFFMAN = 7;
    const int DECODE_HUFFMAN_LENBITS = 8;
    const int DECODE_HUFFMAN_DIST = 9;
    const int DECODE_HUFFMAN_DISTBITS = 10;
    const int FINISHED = 12;
    int mode;
    int neededBits;
    int repLength, repDist;
    int uncomprLen;
    bool isLastBlock;
    int totalIn;
    readonly StreamManipulator input;
    readonly OutputWindow outputWindow;
    InflaterDynHeader dynHeader;
    InflaterHuffmanTree litlenTree, distTree;
    public Inflater()
    {
      input = new StreamManipulator();
      outputWindow = new OutputWindow();
      mode = DECODE_BLOCKS;
    }
    bool DecodeHuffman()
    {
      int free = outputWindow.GetFreeSpace();
      while (free >= 258)
      {
        int symbol;
        switch (mode)
        {
          case DECODE_HUFFMAN:
          {
            while (((symbol = litlenTree.GetSymbol(input)) & ~0xff) == 0)
            {
              outputWindow.Write(symbol);
              if (--free < 258) return true;
            }
            if (symbol < 257)
            {
              if (symbol < 0) return false;
              distTree = null;
              litlenTree = null;
              mode = DECODE_BLOCKS;
              return true;
            }
            try
            {
              repLength = CPLENS[symbol - 257];
              neededBits = CPLEXT[symbol - 257];
            }
            catch
            {
              throw new FormatException("Illegal rep length code");
            }
            goto case DECODE_HUFFMAN_LENBITS;
          }
          case DECODE_HUFFMAN_LENBITS:
          {
            if (neededBits > 0)
            {
              mode = DECODE_HUFFMAN_LENBITS;
              int i = input.PeekBits(neededBits);
              if (i < 0) return false;
              input.DropBits(neededBits);
              repLength += i;
            }
            mode = DECODE_HUFFMAN_DIST;
            goto case DECODE_HUFFMAN_DIST;
          }
          case DECODE_HUFFMAN_DIST:
          {
            symbol = distTree.GetSymbol(input);
            if (symbol < 0) return false;
            try
            {
              repDist = CPDIST[symbol];
              neededBits = CPDEXT[symbol];
            }
            catch (Exception)
            {
              throw new FormatException("Illegal rep dist code");
            }
            goto case DECODE_HUFFMAN_DISTBITS;
          }
          case DECODE_HUFFMAN_DISTBITS:
          {
            if (neededBits > 0)
            {
              mode = DECODE_HUFFMAN_DISTBITS;
              int i = input.PeekBits(neededBits);
              if (i < 0) return false;
              input.DropBits(neededBits);
              repDist += i;
            }
            outputWindow.Repeat(repLength, repDist);
            free -= repLength;
            mode = DECODE_HUFFMAN;
          } break;
          default: throw new FormatException();
        }
      }
      return true;
    }
    bool Decode()
    {
      switch (mode)
      {
        case DECODE_BLOCKS:
        {
          if (isLastBlock)
          {
            mode = FINISHED;
            return false;
          }
          int type = input.PeekBits(3);
          if (type < 0) return false;
          input.DropBits(3);
          if ((type & 1) != 0) isLastBlock = true;
          switch (type >> 1)
          {
            case DeflaterConstants.StoredBlock:
            {
              input.SkipToByteBoundary();
              mode = DECODE_STORED_LEN1;
            } break;
            case DeflaterConstants.StaticTrees:
            {
              litlenTree = InflaterHuffmanTree.defLitLenTree;
              distTree = InflaterHuffmanTree.defDistTree;
              mode = DECODE_HUFFMAN;
            } break;
            case DeflaterConstants.DynTrees:
            {
              dynHeader = new InflaterDynHeader();
              mode = DECODE_DYN_HEADER;
            } break;
            default: throw new FormatException("Unknown block type " + type);
          }
          return true;
        }
        case DECODE_STORED_LEN1:
        {
          if ((uncomprLen = input.PeekBits(16)) < 0) return false;
          input.DropBits(16);
          mode = DECODE_STORED_LEN2;
          goto case DECODE_STORED_LEN2;
        }
        case DECODE_STORED_LEN2:
        {
          int nlen = input.PeekBits(16);
          if (nlen < 0) return false;
          input.DropBits(16);
          if (nlen != (uncomprLen ^ 0xffff)) throw new FormatException("broken uncompressed block");
          mode = DECODE_STORED;
          goto case DECODE_STORED;
        }
        case DECODE_STORED:
        {
          int more = outputWindow.CopyStored(input, uncomprLen);
          uncomprLen -= more;
          if (uncomprLen == 0)
          {
            mode = DECODE_BLOCKS;
            return true;
          }
          return !input.IsNeedingInput;
        }
        case DECODE_DYN_HEADER:
        {
          if (!dynHeader.Decode(input)) return false;
          litlenTree = dynHeader.BuildLitLenTree();
          distTree = dynHeader.BuildDistTree();
          mode = DECODE_HUFFMAN;
          goto case DECODE_HUFFMAN; /* fall through */
        }
        case DECODE_HUFFMAN:
        case DECODE_HUFFMAN_LENBITS:
        case DECODE_HUFFMAN_DIST:
        case DECODE_HUFFMAN_DISTBITS: return DecodeHuffman();
        case FINISHED: return false;
        default: throw new FormatException();
      }
    }
    public void SetInput(byte[] buf, int off, int len)
    {
      input.SetInput(buf, off, len); totalIn += len;
    }
    public int Inflate(byte[] buf, int off, int len)
    {
      if (len == 0)
      {
        if (!IsFinished) Decode();
        return 0;
      }
      int count = 0;
      do
      {
        int more = outputWindow.CopyOutput(buf, off, len);
        off += more;
        count += more;
        len -= more;
        if (len == 0) return count;
      } while (Decode() || outputWindow.GetAvailable() > 0);
      return count;
    }
    public bool IsNeedingInput
    {
      get
      {
        return input.IsNeedingInput;
      }
    }
    public bool IsFinished
    {
      get
      {
        return mode == FINISHED && outputWindow.GetAvailable() == 0;
      }
    }
  }
}
