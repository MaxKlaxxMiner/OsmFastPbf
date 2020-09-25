using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmFastPbf
{
  /// <summary>
  /// Struktur eines OSM-Blocks
  /// </summary>
  public struct OsmBlob
  {
    static int ReadInt32Fix(byte[] buf, int ofs, out int val)
    {
      val = (int)((uint)buf[ofs] << 24 | (uint)buf[ofs + 1] << 16 | (uint)buf[ofs + 2] << 8 | buf[ofs + 3]);
      return sizeof(int);
    }

    static int ReadUInt32(byte[] buf, int ofs, out uint val)
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

    static int ReadInt64(byte[] buf, int ofs, out long val)
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

    static int ReadString(byte[] buf, int ofs, out string val)
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
    static int ReadString(byte[] buf, int ofs, string val)
    {
      string tmp;
      int len = ReadString(buf, ofs, out tmp);
      if (tmp != val) throw new PbfParseException();
      return len;
    }

    public static int DecodeQuick(byte[] buf, int ofs, out OsmBlob result)
    {
      int len = 0;
      int blobHeaderLen;
      string type;
      uint datasize;
      long rawSize;
      long blockBytes;
      len += ReadInt32Fix(buf, ofs + len, out blobHeaderLen);
      len += ReadString(buf, ofs + len, out type);
      len += ReadUInt32(buf, ofs + len, out datasize);
      int blobLength = len + (int)datasize;
      len += ReadInt64(buf, ofs + len, out rawSize);

      //byte[] data;
      //len += ReadEmbedded(buf, ofs + len, out data);

      result = new OsmBlob();

      return blobLength;
    }
  }
}
