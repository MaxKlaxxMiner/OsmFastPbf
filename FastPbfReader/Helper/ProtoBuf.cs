using System.Text;

namespace OsmFastPbf.Helper
{
  public static class ProtoBuf
  {
    public static int ReadInt32Fix(byte[] buf, int ofs, out int val)
    {
      val = (int)((uint)buf[ofs] << 24 | (uint)buf[ofs + 1] << 16 | (uint)buf[ofs + 2] << 8 | buf[ofs + 3]);
      return sizeof(int);
    }

    public static int ReadUInt32(byte[] buf, int ofs, out uint val)
    {
      int len = 0;
      byte t = buf[ofs + len++];
      if (t != (0 | 3 << 3)) throw new PbfParseException(); // Varint (0), uint32 (3)
      val = buf[ofs + len++];
      if (val > 127)
      {
        val &= 127;
        for (int bit = 7; ; bit += 7)
        {
          byte b = buf[ofs + len++];
          val |= (uint)(b & 127) << bit;
          if (b <= 127) break;
        }
      }
      return len;
    }

    public static int ReadInt64(byte[] buf, int ofs, out long val)
    {
      int len = 0;
      byte t = buf[ofs + len++];
      if (t != (0 | 2 << 3)) throw new PbfParseException(); // Varint (0), int64 (2)
      val = buf[ofs + len++];
      if (val > 127)
      {
        val &= 127;
        for (int bit = 7; ; bit += 7)
        {
          byte b = buf[ofs + len++];
          val |= (long)(b & 127) << bit;
          if (b <= 127) break;
        }
      }
      return len;
    }

    public static int ReadEmbeddedLength(byte[] buf, int ofs, out long val)
    {
      int len = 0;
      byte t = buf[ofs + len++];
      if (t != (2 | 3 << 3)) throw new PbfParseException(); // Length-delimited (2), embedded message (3)
      val = buf[ofs + len++];
      if (val > 127)
      {
        val &= 127;
        for (int bit = 7; ; bit += 7)
        {
          byte b = buf[ofs + len++];
          val |= (long)(b & 127) << bit;
          if (b <= 127) break;
        }
      }
      return len;
    }

    public static int ReadString(byte[] buf, int ofs, out string val)
    {
      int len = 0;
      byte t = buf[ofs + len++];
      if (t != (2 | 1 << 3)) throw new PbfParseException(); // Length-delimited (2), string (1)
      int size = buf[ofs + len++];
      if (size > 127)
      {
        size &= 127;
        for (int bit = 7; ; bit += 7)
        {
          byte b = buf[ofs + len++];
          size |= (b & 127) << bit;
          if (b <= 127) break;
        }
      }
      val = Encoding.UTF8.GetString(buf, ofs + len, size);
      len += size;
      return len;
    }
  }
}
