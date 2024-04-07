using System;

namespace ConsumeCdc.Library;

public class CdcException : Exception
{
    public CdcException() { }
    public CdcException(string message) : base(message) { }
    public CdcException(string? message, Exception? innerException) : base(message, innerException) { }
}
