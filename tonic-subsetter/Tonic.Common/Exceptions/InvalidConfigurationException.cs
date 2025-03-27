using System;

namespace Tonic.Common.Exceptions;

public class InvalidConfigurationException : Exception
{
    public InvalidConfigurationException(string message) : base(FormatMessage(message))
    {
    }

    private static string FormatMessage(string message) => $"Invalid Configuration: " + message;
}