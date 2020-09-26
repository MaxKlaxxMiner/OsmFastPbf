// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf;
using OsmFastPbf.Helper;
using OsmFastPbf.zlibTuned;

namespace TestTool
{
  partial class Program
  {
    static void Main(string[] args)
    {
      //BufferTest();

      #region # // --- old ---
      //static int ReadEmbedded(byte[] buf, int ofs, out byte[] val)
      //{
      //  int len = 0;
      //  byte t = buf[ofs + len++];
      //  if (t != (2 | 3 << 3)) throw new PbfParseException(); // Length-delimited (2), embedded message (3)
      //  int size = buf[ofs + len++];
      //  if (size > 127)
      //  {
      //    size &= 127;
      //    for (int bit = 7; ; bit += 7)
      //    {
      //      byte b = buf[ofs + len++];
      //      size |= (b & 127) << bit;
      //      if (b <= 127) break;
      //    }
      //  }
      //  val = new byte[size];
      //  Array.Copy(buf, ofs + len, val, 0, size); len += size;
      //  return len;
      //}
      //static int ReadEmbeddedCompressed(byte[] buf, int ofs, out byte[] val)
      //{
      //  int len = ReadEmbedded(buf, ofs, out val);
      //  using (var zlib = new InflaterInputStream(new RamStream(val))) val = zlib.ReadAllBytes(true);
      //  return len;
      //}

      //static int ReadPacket(byte[] buf, int ofs, out List<ulong> val)
      //{
      //  int len = 0;
      //  byte t = buf[ofs + len++];
      //  if (t != (2 | 4 << 3) && t != (2 | 1 << 3) && t != (2 | 5 << 3) && t != (2 | 16 << 3)) throw new PbfParseException(); // Length-delimited (2), *
      //  int size = buf[ofs + len++];
      //  if (size > 127)
      //  {
      //    size &= 127;
      //    for (int bit = 7; ; bit += 7)
      //    {
      //      byte b = buf[ofs + len++];
      //      size |= (b & 127) << bit;
      //      if (b <= 127) break;
      //    }
      //  }
      //  int end = len + size;
      //  var vals = new List<ulong>();
      //  while (len < end)
      //  {
      //    ulong v = buf[ofs + len++];
      //    if (v > 127)
      //    {
      //      v &= 127;
      //      for (int bit = 7; ; bit += 7)
      //      {
      //        byte b = buf[ofs + len++];
      //        v |= (ulong)(b & 127) << bit;
      //        if (b <= 127) break;
      //      }
      //    }
      //    vals.Add(v);
      //  }
      //  val = vals;
      //  return len;
      //}
      //static int ReadPacket(byte[] buf, int ofs, out string val)
      //{
      //  List<ulong> tmp;
      //  int len = ReadPacket(buf, ofs, out tmp);
      //  val = new string(tmp.SelectArray(c => checked((char)c)));
      //  return len;
      //}
      //static int ReadPacket(byte[] buf, int ofs, out ulong[] val)
      //{
      //  List<ulong> tmp;
      //  int len = ReadPacket(buf, ofs, out tmp);
      //  val = tmp.ToArray();
      //  return len;
      //}
      //static int ReadPacket(byte[] buf, int ofs, string val)
      //{
      //  string tmp;
      //  int len = ReadPacket(buf, ofs, out tmp);
      //  if (tmp != val) throw new PbfParseException();
      //  return len;
      //}

      //public struct OSMHeader
      //{
      //  public OSMHeader(byte[] buf)
      //  {
      //    using (var zlib = new InflaterInputStream(new RamStream(buf))) buf = zlib.ReadAllBytes(true);

      //    int pos = 0;
      //    ulong[] bbox;
      //    pos += ReadPacket(buf, pos, out bbox);
      //    pos += ReadPacket(buf, pos, "OsmSchema-V0.6");
      //    pos += ReadPacket(buf, pos, "DenseNodes");
      //    pos += ReadPacket(buf, pos, "Has_Metadata");
      //    pos += ReadPacket(buf, pos, "Sort.Type_then_ID");
      //    pos += ReadPacket(buf, pos, "\x14");
      //  }
      //}
      #endregion

      string path = "planet-latest.osm.pbf";
      for (int i = 0; i < 32 && !File.Exists(path); i++) path = "../" + path;
      if (!File.Exists(path)) throw new FileNotFoundException(path.TrimStart('.', '/'));
      using (var test = new FastPbfReader(path))
      {
        #region # // --- Index einlesen ---
        test.RandomBuffering = true;

        long pos = 0;
        var buf = test.buffer;
        int tim = 0;

        var blocks = new List<OsmBlob>();
        for (; ; )
        {
          if (tim != Environment.TickCount)
          {
            tim = Environment.TickCount;
            Console.WriteLine("{0:N0} / {1:N0}", pos, test.pbfSize);
          }
          int ofs = test.PrepareBuffer(pos, 32);
          OsmBlob blob;
          OsmBlob.DecodeQuick(buf, ofs, out blob);
          blob.pbfOfs = pos;
          pos += blob.blobLen;
          blocks.Add(blob);
          if (pos >= test.pbfSize) break;
        }

        Console.WriteLine();
        Console.WriteLine("            Blocks: {0,15:N0}", blocks.Count);
        Console.WriteLine("    PBF-Compressed: {0,15:N0} Bytes", blocks.Sum(blob => (long)blob.blobLen));
        Console.WriteLine("  PBF-Uncompressed: {0,15:N0} Bytes", blocks.Sum(blob => (long)(blob.blobLen - blob.dataZipLen + blob.dataLen)));
        Console.WriteLine();
        #endregion

        test.RandomBuffering = false;
        {
          var blob = blocks[0];
          int ofs = test.PrepareBuffer(blob.pbfOfs + blob.dataZipOfs, blob.dataZipLen);
          var outputBuf1 = new byte[16 * 1048576];
          int bytes1;
          var outputBuf2 = new byte[16 * 1048576];
          int bytes2;
          using (var mem = new MemoryStream(buf, ofs + 2, blob.dataZipLen - 2))
          using (var inf = new FastInflaterStream(mem))
          {
            bytes1 = inf.Read(outputBuf1, 0, outputBuf1.Length);
          }
          bytes2 = ProtoBuf.FastInflate(buf, ofs, blob.dataZipLen, outputBuf2, 0);
        }
      }
    }
  }
}
