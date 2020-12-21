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
    static void ConverterTest()
    {
      using (var pbf = new OsmPbfReader(PbfPath))
      {
        long cc = 0;

        var bufFull = new byte[2000 * 1048576];
        int lenFull = 0;
        long lastFullId = 0;
        long lastFullPos = 0;
        int blockFull = 0;

        var buf = new byte[2000 * 1048576];
        int len = 0;
        long lastId = 0;
        long lastPos = 0;
        int block = 0;

        long totalPos = 0;
        long totalSum = pbf.nodeIndex.Sum(x => x.nodeCount);

        foreach (var node in pbf.ReadAllNodes())
        {
          totalPos++;

          var gpsPos = new GpsPos(node);

          lenFull += ProtoBuf.WriteVarInt(bufFull, lenFull, ProtoBuf.UnsignedInt64(node.id - lastFullId));
          lastFullId = node.id;
          long nextFullPos = gpsPos.Int64Pos;
          lenFull += ProtoBuf.WriteVarInt(bufFull, lenFull, ProtoBuf.UnsignedInt64(nextFullPos - lastFullPos));
          lastFullPos = nextFullPos;

          if (lenFull > bufFull.Length - 18)
          {
            blockFull++;
            using (var wdat = File.Create("../tmp/node_blockfull_" + blockFull + "_" + lastFullId + ".dat"))
            {
              wdat.Write(bufFull, 0, lenFull);
              lenFull = 0;
              lastFullId = 0;
              lastFullPos = 0;
            }
          }

          if (node.values.Length > 0)
          {
            cc++;
            if ((ushort)cc == 0) Console.WriteLine(cc.ToString("N0") + " (" + (100.0 / totalSum * totalPos).ToString("N2") + " %) - " + (len / 1048576.0).ToString("N1") + " MByte / " + (lenFull / 1048576.0).ToString("N1") + " MByte");
            len += ProtoBuf.WriteVarInt(buf, len, ProtoBuf.UnsignedInt64(node.id - lastId));
            lastId = node.id;
            long nextPos = gpsPos.Int64Pos;
            len += ProtoBuf.WriteVarInt(buf, len, ProtoBuf.UnsignedInt64(nextPos - lastPos));
            lastPos = nextPos;
            len += ProtoBuf.WriteVarInt(buf, len, (uint)node.values.Length);
            foreach (var v in node.values)
            {
              len += ProtoBuf.WriteString(buf, len, v.Key);
              len += ProtoBuf.WriteString(buf, len, v.Value);
            }
            if (len > buf.Length - 65536)
            {
              block++;
              using (var wdat = File.Create("../tmp/node_block_" + block + "_" + lastId + ".dat"))
              {
                wdat.Write(buf, 0, len);
                len = 0;
                lastId = 0;
                lastPos = 0;
              }
            }
          }
        }
        if (lenFull > 0)
        {
          blockFull++;
          using (var wdat = File.Create("../tmp/node_blockfull_" + blockFull + "_" + lastFullId + ".dat"))
          {
            wdat.Write(bufFull, 0, lenFull);
          }
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
