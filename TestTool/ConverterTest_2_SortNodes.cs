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
    struct DecodeNodeBlockValue
    {
      public long id;
      public long pos;
      public int valuePos;
      public int valueCount;
      public override string ToString()
      {
        return new { id, pos = new GpsPos(pos), valueCount }.ToString();
      }
    }

    static void WriteNodeBlock(Dictionary<string, int> keyDict, Dictionary<string, int> valDict, List<int> outputValues, List<DecodeNodeBlockValue> outputNodes, long lastId)
    {
      Func<Stream, byte[], int, int> write = (writeStream, buffer, len) =>
      {
        if (len > 0)
        {
          writeStream.Write(buffer, 0, len);
        }
        return -len;
      };

      using (var wdat = File.Create("../tmp/node_sorted_" + lastId + ".dat"))
      {
        var buf = new byte[1048576];
        int p = 8;

        p += ProtoBuf.WriteVarInt(buf, p, (uint)keyDict.Count);
        foreach (var k in keyDict.OrderBy(x => x.Value).Select(x => x.Key))
        {
          p += ProtoBuf.WriteString(buf, p, k);
          if (p > 1000000) p += write(wdat, buf, p);
        }
        p += ProtoBuf.WriteVarInt(buf, p, (uint)valDict.Count);
        foreach (var k in valDict.OrderBy(x => x.Value).Select(x => x.Key))
        {
          p += ProtoBuf.WriteString(buf, p, k);
          if (p > 1000000) p += write(wdat, buf, p);
        }
        p += write(wdat, buf, p);
        wdat.Position = 0;
        wdat.Write(BitConverter.GetBytes((int)wdat.Length), 0, sizeof(int));
        wdat.Position = wdat.Length;

        p += ProtoBuf.WriteVarInt(buf, p, (uint)(outputValues.Count / 2));
        foreach (var v in outputValues)
        {
          p += ProtoBuf.WriteVarInt(buf, p, (uint)v);
          if (p > 1000000) p += write(wdat, buf, p);
        }
        p += write(wdat, buf, p);
        wdat.Position = 4;
        wdat.Write(BitConverter.GetBytes((int)wdat.Length), 0, sizeof(int));
        wdat.Position = wdat.Length;

        p += ProtoBuf.WriteVarInt(buf, p, (uint)outputNodes.Count);
        long pos = 0;
        long id = 0;
        int valuePos = 0;
        foreach (var n in outputNodes)
        {
          p += ProtoBuf.WriteVarInt(buf, p, ProtoBuf.UnsignedInt64(n.pos - pos));
          pos = n.pos;
          p += ProtoBuf.WriteVarInt(buf, p, ProtoBuf.UnsignedInt64(n.id - id));
          id = n.id;
          p += ProtoBuf.WriteVarInt(buf, p, ProtoBuf.UnsignedInt64(n.valuePos - valuePos));
          valuePos = n.valuePos;
          p += ProtoBuf.WriteVarInt(buf, p, (uint)n.valueCount);
          if (p > 1000000) p += write(wdat, buf, p);
        }
        write(wdat, buf, p);
      }
    }

    static void DecodeNodeBlock(byte[] buf, int bufLen)
    {
      long id = 0;
      long pos = 0;

      var keyDict = new Dictionary<string, int>();
      var valDict = new Dictionary<string, int>();

      var outputNodes = new List<DecodeNodeBlockValue>();
      var outputValues = new List<int>();

      for (int p = 0; p < bufLen; )
      {
        DecodeNodeBlockValue nodeValue;
        ulong tmp;
        p += ProtoBuf.ReadVarInt(buf, p, out tmp);
        id += ProtoBuf.SignedInt64(tmp);
        nodeValue.id = id;
        p += ProtoBuf.ReadVarInt(buf, p, out tmp);
        pos += ProtoBuf.SignedInt64(tmp);
        nodeValue.pos = pos;
        p += ProtoBuf.ReadVarInt(buf, p, out tmp);
        int valueCount = (int)(uint)tmp;
        nodeValue.valueCount = valueCount;
        nodeValue.valuePos = outputValues.Count / 2;
        for (int i = 0; i < valueCount; i++)
        {
          string key, val;
          p += ProtoBuf.ReadString(buf, p, out key);
          if (!keyDict.ContainsKey(key)) keyDict.Add(key, keyDict.Count);
          outputValues.Add(keyDict[key]);
          p += ProtoBuf.ReadString(buf, p, out val);
          if (!valDict.ContainsKey(val)) valDict.Add(val, valDict.Count);
          outputValues.Add(valDict[val]);
        }
        outputNodes.Add(nodeValue);
      }

      // --- sort ---
      long lastId = outputNodes[outputNodes.Count - 1].id;
      Console.WriteLine(" sort: " + outputNodes.Count + " nodes");
      outputNodes.Sort((x, y) => x.pos.CompareTo(y.pos));

      // --- write ---
      Console.WriteLine("write: " + outputNodes.Count + " nodes");
      WriteNodeBlock(keyDict, valDict, outputValues, outputNodes, lastId);
    }

    static void ConverterTest_2_SortNodes()
    {
      var blockFiles = Directory.GetFiles("../tmp/", "node_block_*_*.dat");
      if (blockFiles.Length == 0) throw new Exception();

      var buf = new byte[2000 * 1048576];

      Array.Sort(blockFiles, (x, y) => int.Parse(x.Split('_').Reverse().Skip(1).First()) - int.Parse(y.Split('_').Reverse().Skip(1).First()));
      foreach (var blockFile in blockFiles)
      {
        Console.WriteLine("read : " + blockFile);
        int bufLen;
        using (var rdat = File.OpenRead(blockFile))
        {
          bufLen = rdat.Read(buf, 0, buf.Length);
          if (bufLen != rdat.Length) throw new Exception();
        }
        Console.WriteLine("parse: " + blockFile);
        DecodeNodeBlock(buf, bufLen);
        File.Delete(blockFile);
      }
    }
  }
}
