using System;
using OsmFastPbf.Helper;
// ReSharper disable UselessBinaryOperation

namespace OsmFastPbf
{
  public class PbfFast
  {
    public static int DecodeInfo(byte[] buf, int ofs)
    {
      /*****
       * message Info
       * {
       *   optional int32 version = 1 [default = -1];
       *   optional int64 timestamp = 2;
       *   optional int64 changeset = 3;
       *   optional int32 uid = 4;
       *   optional uint32 user_sid = 5; // String IDs
       *   
       *   // The visible flag is used to store history information. It indicates that
       *   // the current object version has been created by a delete operation on the
       *   // OSM API.
       *   // When a writer sets this flag, it MUST add a required_features tag with
       *   // value "HistoricalInformation" to the HeaderBlock.
       *   // If this flag is not available for some object it MUST be assumed to be
       *   // true if the file has the required_features tag "HistoricalInformation"
       *   // set.
       *   optional bool visible = 6;
       * }
       *****/

      int len = 0;
      ulong dataLen;
      len += ProtoBuf.ReadVarInt(buf, ofs + len, out dataLen);
      int endLen = len + (int)dataLen;
      ulong tmp;

      // --- optional int32 version = 1 [default = -1]; ---
      if (buf[ofs + len] == (1 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //int version = (int)(uint)tmp;
      }

      // --- optional int64 timestamp = 2; ---
      if (buf[ofs + len] == (2 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //long timestamp = (long)tmp;
      }

      // --- optional int64 changeset = 3; ---
      if (buf[ofs + len] == (3 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //long changeset = (long)tmp;
      }

      // --- optional int32 uid = 4; ---
      if (buf[ofs + len] == (4 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //int uid = (int)(uint)tmp;
      }

      // --- optional uint32 user_sid = 5; // String IDs ---
      if (buf[ofs + len] == (5 << 3 | 0))
      {
        len++;
        len += ProtoBuf.ReadVarInt(buf, ofs + len, out tmp);
        //uint userSid = (uint)tmp;
      }

      // --- optional bool visible = 6; ---
      if (buf[ofs + len] == (6 << 3 | 0))
      {
        throw new NotSupportedException();
      }

      if (len != endLen) throw new PbfParseException();

      return len;
    }
  }
}
