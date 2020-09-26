using System;

namespace OsmFastPbf
{
  public sealed class PbfParseException : Exception
  {
    public PbfParseException() { }
    public PbfParseException(string message) : base(message) { }
  }
}
