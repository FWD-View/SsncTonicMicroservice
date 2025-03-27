using System;

namespace Tonic.Common.OracleHelper.Exceptions
{
    public class OracleToolTransientException : Exception
    {
        public OracleToolTransientException(string friendlyMessage) : base(friendlyMessage)
        {
        }

        public OracleToolTransientException(string friendlyMessage, Exception exception) : base(friendlyMessage, exception)
        {
        }
    }
}