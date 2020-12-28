// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmFastPbf;
using OsmFastPbf.Helper;

namespace TestTool
{
  partial class Program
  {
    static void ConverterTest_1_ExtractNodes()
    {
      Directory.CreateDirectory("../tmp");
      using (var wdatRaw = File.Create("../tmp/node_rawfull.dat"))
      using (var wdatRawIndex = File.Create("../tmp/node_rawfull_index.dat"))
      using (var pbf = new OsmPbfReader(PbfPath))
      {
        long cc = 0;

        var rawBuf = new byte[4096];
        int rawLen = 0;
        long rawId = 0;
        long rawPos = 0;

        var buf = new byte[2000 * 1048576];
        int len = 0;
        long lastId = 0;
        long lastPos = 0;
        int block = 0;

        long totalPos = 0;
        long totalSum = pbf.nodeIndex.Sum(x => x.nodeCount);

        wdatRawIndex.Write(BitConverter.GetBytes(0L), 0, sizeof(long));
        wdatRawIndex.Write(BitConverter.GetBytes(0L), 0, sizeof(long));
        foreach (var node in pbf.ReadAllNodes())
        {
          totalPos++;
          var gpsPos = new GpsPos(node);

          rawLen += ProtoBuf.WriteVarInt(rawBuf, rawLen, ProtoBuf.UnsignedInt64(node.id - rawId));
          rawId = node.id;
          long nextFullPos = gpsPos.Int64Pos;
          rawLen += ProtoBuf.WriteVarInt(rawBuf, rawLen, ProtoBuf.UnsignedInt64(nextFullPos - rawPos));
          rawPos = nextFullPos;

          if (rawLen > rawBuf.Length - 18)
          {
            while (rawLen < rawBuf.Length) rawBuf[rawLen++] = 0;
            wdatRaw.Write(rawBuf, 0, rawBuf.Length);
            wdatRawIndex.Write(BitConverter.GetBytes(rawId+1), 0, sizeof(long));
            wdatRawIndex.Write(BitConverter.GetBytes(wdatRaw.Length), 0, sizeof(long));
            rawLen = 0;
            rawId = 0;
            rawPos = 0;
          }

          //todo
          //if (node.values.Length > 0)
          //{
          //  cc++;
          //  if ((ushort)cc == 0) Console.WriteLine(cc.ToString("N0") + " (" + (100.0 / totalSum * totalPos).ToString("N2") + " %) - " + (len / 1048576.0).ToString("N1") + " MByte / " + (wdatRaw.Length / 1048576.0).ToString("N1") + " MByte");
          //  len += ProtoBuf.WriteVarInt(buf, len, ProtoBuf.UnsignedInt64(node.id - lastId));
          //  lastId = node.id;
          //  long nextPos = gpsPos.Int64Pos;
          //  len += ProtoBuf.WriteVarInt(buf, len, ProtoBuf.UnsignedInt64(nextPos - lastPos));
          //  lastPos = nextPos;
          //  len += ProtoBuf.WriteVarInt(buf, len, (uint)node.values.Length);
          //  foreach (var v in node.values)
          //  {
          //    len += ProtoBuf.WriteString(buf, len, v.Key);
          //    len += ProtoBuf.WriteString(buf, len, v.Value);
          //  }
          //  if (len > buf.Length - 65536)
          //  {
          //    block++;
          //    using (var wdat = File.Create("../tmp/node_block_" + block + "_" + lastId + ".dat"))
          //    {
          //      wdat.Write(buf, 0, len);
          //      len = 0;
          //      lastId = 0;
          //      lastPos = 0;
          //    }
          //  }
          //}
        }
        if (rawLen > 0)
        {
          while (rawLen < rawBuf.Length) rawBuf[rawLen++] = 0;
          wdatRaw.Write(rawBuf, 0, rawBuf.Length);
        }
        if (len > 0)
        {
          block++;
          using (var wdat = File.Create("../tmp/node_block_" + block + "_" + lastId + ".dat"))
          {
            wdat.Write(buf, 0, len);
          }
        }

        //foreach (var way in pbf.ReadAllWays())
        //{
        //  Console.WriteLine(way);
        //}
        //foreach (var relation in pbf.ReadAllRelations())
        //{
        //  cc++;
        //  if ((byte)cc == 0)
        //  {
        //    Console.WriteLine(cc.ToString("N0") + " - " + relation);
        //  }
        //}
      }
    }
  }
}
