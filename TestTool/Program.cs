// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf;

namespace TestTool
{
  partial class Program
  {
    static void Main(string[] args)
    {
      //BufferTest();

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

      using (var test = new FastPbfReader("../../../../planet-latest.osm.pbf"))
      {
        long pos = 0;
        var buf = test.buffer;

        for (int i = 0; i < 1000; i++)
        {
          int ofs = test.PrepareBuffer(pos, 1024);
          OsmBlob header;
          pos += OsmBlob.DecodeQuick(buf, ofs, out header);
        }
      }
    }
  }
}
