using System;

namespace OsmFastPbf.zlibTuned
{
  /// <summary>
  /// Klasse zum komprimieren von Daten nach dem Deflate-Algorythmus
  /// </summary>
  public sealed class Deflater
  {
    const int BestCompression = 10;
    const int NoCompression = 0;
    public const int Deflated = 8;
    const int IsFlushing = 0x04;
    const int IsFinishing = 0x08;
    const int BusyState = 0x10;
    const int FlushingState = 0x14;
    const int FinishingState = 0x1c;
    const int FinishedState = 0x1e;
    const int ClosedState = 0x7f;
    readonly int level;
    int state;
    readonly PendingBuffer pending;
    readonly DeflaterEngine engine;

    public unsafe Deflater(int level, int tooFar)
    {
      if (level < NoCompression || level > BestCompression) throw new ArgumentOutOfRangeException("level");
      if (tooFar < 0 || tooFar > 32767) throw new ArgumentOutOfRangeException("tooFar");
      this.level = level;
      pending = new PendingBuffer(DeflaterConstants.PendingBufSize);
      engine = new DeflaterEngine(pending, level, tooFar);
      state = BusyState;
      pending.Reset();
      fixed (byte* mem = engine.memFix)
      {
        engine.Reset(mem);
      }
    }

    public unsafe void Reset()
    {
      state = BusyState;
      pending.Reset();
      fixed (byte* mem = engine.memFix)
      {
        engine.Reset(mem);
      }
    }

    public int TotalIn
    {
      get
      {
        return engine.TotalIn;
      }
    }

    public void Flush()
    {
      state |= IsFlushing;
    }

    public void Finish()
    {
      state |= IsFlushing | IsFinishing;
    }

    public bool IsFinished
    {
      get
      {
        return state == FinishedState && pending.IsFlushed;
      }
    }

    public bool IsNeedingInput
    {
      get
      {
        return engine.NeedsInput();
      }
    }

    public void SetInput(byte[] input, int off, int len)
    {
      if ((state & IsFinishing) != 0) throw new InvalidOperationException("finish()/end() already called");
      engine.SetInput(input, off, len);
    }

    public unsafe int Deflate(byte[] output, int offset, int length)
    {
      int origLength = length;
      if (state == ClosedState) throw new InvalidOperationException("Deflater closed");
      if (state < BusyState)
      {
        int header = (Deflated + ((DeflaterConstants.MaxWbits - 8) << 4)) << 8;
        int levelFlags = (level - 1) >> 1;
        if (levelFlags < 0 || levelFlags > 3) levelFlags = 3;
        header |= levelFlags << 6;
        header += 31 - (header % 31);
        pending.WriteShortMsb(header);
        state = BusyState | (state & (IsFlushing | IsFinishing));
      }
      fixed (byte* mem = engine.memFix)
      {
        for (; ; )
        {
          int count = pending.Flush(output, offset, length);
          offset += count;
          length -= count;
          if (length == 0 || state == FinishedState) break;
          if (engine.Deflate((state & IsFlushing) != 0, (state & IsFinishing) != 0, mem)) continue;
          switch (state)
          {
            case BusyState: return origLength - length;
            case FlushingState:
            {
              if (level != NoCompression)
              {
                int neededbits = 8 + ((-pending.BitCount) & 7);
                while (neededbits > 0)
                {
                  pending.WriteBits(2, 10);
                  neededbits -= 10;
                }
              }
              state = BusyState;
            } break;
            case FinishingState:
            {
              pending.AlignToByte();
              state = FinishedState;
            } break;
          }
        }
      }
      return origLength - length;
    }
  }
}
