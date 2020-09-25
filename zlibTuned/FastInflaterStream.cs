using System;
using System.IO;
// ReSharper disable UnusedMember.Global

namespace OsmFastPbf.zlibTuned
{
  /// <summary>
  /// Stream-Klasse zum entpacken von Deflate-komprimierten Streams (wird z.B. von GnuZip und Zip verwendet)
  /// </summary>
  // ReSharper disable once ClassCanBeSealed.Global
  public class FastInflaterStream : Stream
  {
    readonly FastInflater.Inflater inf;
    readonly byte[] buf;
    int len;
    readonly byte[] onebytebuffer = new byte[1];
    readonly Stream baseInputStream;
    public override bool CanRead
    {
      get
      {
        return baseInputStream.CanRead;
      }
    }
    public override bool CanSeek
    {
      get
      {
        return false;
      }
    }
    public override bool CanWrite
    {
      get
      {
        return baseInputStream.CanWrite;
      }
    }
    public override long Length
    {
      get
      {
        return len;
      }
    }
    public override long Position
    {
      get
      {
        return baseInputStream.Position;
      }
      set
      {
        baseInputStream.Position = value;
      }
    }
    public override void Flush()
    {
      baseInputStream.Flush();
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotSupportedException("Seek not supported");
    }
    public override void SetLength(long val)
    {
      baseInputStream.SetLength(val);
    }
    public override void Write(byte[] array, int offset, int count)
    {
      baseInputStream.Write(array, offset, count);
    }
    public override void WriteByte(byte val)
    {
      baseInputStream.WriteByte(val);
    }
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
    {
      throw new NotSupportedException("Asynch write not currently supported");
    }
    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="baseInputStream">BasisStream zum lesen der Daten</param>
    /// <param name="size">Größe des Buffers</param>
    public FastInflaterStream(Stream baseInputStream, int size = 4096)
    {
      this.baseInputStream = baseInputStream;
      inf = new FastInflater.Inflater();
      try
      {
        len = (int)baseInputStream.Length;
      }
      catch
      {
        len = 0;
      }
      if (size <= 0) throw new ArgumentOutOfRangeException();
      buf = new byte[size];
    }
    public override void Close()
    {
      baseInputStream.Close();
    }
    /// <summary>
    /// füllt die Daten
    /// </summary>
    void Fill()
    {
      len = baseInputStream.Read(buf, 0, buf.Length);
      if (len <= 0) throw new ApplicationException("Deflated stream ends early.");
      inf.SetInput(buf, 0, len);
    }
    public override int ReadByte()
    {
      int nread = Read(onebytebuffer, 0, 1);
      if (nread > 0) return onebytebuffer[0];
      return -1;
    }
    public override int Read(byte[] b, int off, int length)
    {
      for (; ; )
      {
        int count = inf.Inflate(b, off, length);
        if (count > 0) return count;
        if (inf.IsFinished) return 0;
        if (!inf.IsNeedingInput) return 0;
        Fill();
      }
    }
  }
}
