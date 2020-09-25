using System;
using System.IO;

namespace OsmFastPbf.zlibTuned
{
  /// <summary>
  /// Stream-Klasse zum komprimieren nach einem Deflate-Stream (wird z.B. von GnuZip und Zip verwendet)
  /// </summary>
  public class DeflaterXOutputStream : Stream
  {
    readonly byte[] buf;
    protected readonly Deflater def;
    protected readonly Stream baseOutputStream;

    public override bool CanRead { get { return baseOutputStream.CanRead; } }

    public override bool CanSeek { get { return false; } }

    public override bool CanWrite { get { return baseOutputStream.CanWrite; } }

    public override long Length { get { return baseOutputStream.Length; } }

    public override long Position { get { return baseOutputStream.Position; } set { baseOutputStream.Position = value; } }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotSupportedException("Seek not supported");
    }

    public override void SetLength(long val)
    {
      baseOutputStream.SetLength(val);
    }

    public override int ReadByte()
    {
      return baseOutputStream.ReadByte();
    }

    public override int Read(byte[] b, int off, int len)
    {
      return baseOutputStream.Read(b, off, len);
    }

    /// <summary>
    /// erstellt den Deflate-Vorgang
    /// </summary>
    void Deflate()
    {
      while (!def.IsNeedingInput)
      {
        int len = def.Deflate(buf, 0, buf.Length);
        if (len <= 0) break;
        baseOutputStream.Write(buf, 0, len);
      }
      if (!def.IsNeedingInput) throw new ApplicationException("Can't deflate all input?");
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="baseOutputStream">Basis-Stream in welchen geschrieben werden soll</param>
    /// <param name="defl">Deflater-Klasse</param>
    /// <param name="bufsize">Größe des Buffers in Bytes</param>
    protected DeflaterXOutputStream(Stream baseOutputStream, Deflater defl, int bufsize)
    {
      this.baseOutputStream = baseOutputStream;
      if (bufsize <= 0) throw new InvalidOperationException("bufsize <= 0");
      buf = new byte[bufsize];
      def = defl;
    }

    public override void Flush()
    {
      def.Flush();
      Deflate();
      baseOutputStream.Flush();
    }

    /// <summary>
    /// beendet der Vorgang
    /// </summary>
    public virtual void Finish()
    {
      def.Finish();
      while (!def.IsFinished)
      {
        int len = def.Deflate(buf, 0, buf.Length);
        if (len <= 0) break;
        baseOutputStream.Write(buf, 0, len);
      }
      if (!def.IsFinished) throw new ApplicationException("Can't deflate all input?");
      baseOutputStream.Flush();
    }

    public override void Close()
    {
      Finish();
      baseOutputStream.Close();
    }

    public override void WriteByte(byte bval)
    {
      var b = new byte[1];
      b[0] = bval;
      Write(b, 0, 1);
    }

    public override void Write(byte[] buffer, int off, int len)
    {
      def.SetInput(buffer, off, len);
      Deflate();
    }
  }
}
