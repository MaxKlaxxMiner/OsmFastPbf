using System;

namespace OsmFastPbf
{
  sealed class PbfParseException : Exception
  {
    public PbfParseException() { }
    public PbfParseException(string message) : base(message) { }
  }
}
