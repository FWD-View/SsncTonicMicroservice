using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using Tonic.Common.Enums;

namespace Tonic.Common.OracleHelper.ErrorCodes;

public enum TransientExceptionType
{
    [TransientErrorCode(TransientErrorCodes.Unmapped, Unmapped)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Unmapped, Unmapped)]
    Unmapped = TransientErrorCodes.Unmapped,

    [TransientErrorCode(TransientErrorCodes.None, None)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.SuccessfulCompletion, None)]
    None = 0,

    /// <inheritdoc cref="System.IO.IOException"/>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [TransientErrorCode(TransientErrorCodes.COR_E_IO, IOException)]
    IOException,
    /// <inheritdoc cref="System.Net.Sockets.SocketException"/>
    //[TransientErrorCode($"{nameof(System)}.{nameof(System.Net)}.{nameof(System.Net.Sockets)}.{nameof(SocketError)}")]
    [TransientErrorCode(typeof(SocketError), SocketException)]
    SocketException,
    /// <inheritdoc cref="System.TimeoutException"/>
    [TransientErrorCode(TransientErrorCodes.COR_E_TIMEOUT, TimeoutException)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.Timeout.Connection, TimeoutException)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.Timeout.ClientRequest, TimeoutException)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.Timeout.Send, TimeoutException)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.Timeout.Receive, TimeoutException)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.Timeout.InboundConnection, TimeoutException)]
    TimeoutException,
    
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.SqlClientUnableToEstablishSqlConnection, SqlClientUnableToEstablishSqlConnection)]
    SqlClientUnableToEstablishSqlConnection,
    
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.ConnectionDoesNotExist, ConnectionDoesNotExist)]
    ConnectionDoesNotExist,
    
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.ConnectionFailure.EndOfFile, ConnectionFailure)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.ConnectionFailure.ConnectionLost, ConnectionFailure)]
    ConnectionFailure,
    
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.DeadlockDetected.LockObject, DeadlockDetected)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.DeadlockDetected.Resource, DeadlockDetected)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.DeadlockDetected.ServersBlocked, DeadlockDetected)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.DeadlockDetected.DistributedTransactionLockTimeout, DeadlockDetected)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.DeadlockDetected.UndoSegmentString, DeadlockDetected)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.DeadlockDetected.ResourceTimeout, DeadlockDetected)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.DeadlockDetected.Resource2, DeadlockDetected)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.DeadlockDetected.Deadlock, DeadlockDetected)]
    DeadlockDetected,
    
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.InsufficientResources, InsufficientResources)]
    InsufficientResources,
    
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.OutOfMemory.CouldNotAllocate, OutOfMemory)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.OutOfMemory.InternalArray, OutOfMemory)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.OutOfMemory.SegmentList, OutOfMemory)]
    OutOfMemory,
    
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.TooManyConnections.TooMany, TooManyConnections)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.TooManyConnections.MaximumExceeded, TooManyConnections)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.TooManyConnections.NoMoreAvailable, TooManyConnections)]
    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.TooManyConnections.InternalLimitExceeded, TooManyConnections)]
    TooManyConnections,

    [TransientErrorCode(DatabaseType.Oracle, OracleErrorCodes.Transient.LockNotAvailable, LockNotAvailable)]
    LockNotAvailable,
}

public static class TransientExceptionTypes
{
    /// <summary>
    /// Return an error code defined in <see cref="TransientExceptionType"/> and mapped via <see cref="TransientErrorCodeAttribute"/>
    /// or `null` when the specified `<paramref name="errorCode"/>` is NOT mapped
    /// </summary>
    public static ITransientErrorCode? FromErrorCode(DatabaseType? databaseType, object errorCode)
    {
        ITransientErrorCode? result = null;

        var codesForDatabaseTypeOrDatabaseAgnostic = TransientErrorCodes.AllCodes
            .SelectMany(x => x.ErrorCodes.Where(y => y.DatabaseType == null || Equals(y.DatabaseType, databaseType)));

        foreach (var transientErrorCode in codesForDatabaseTypeOrDatabaseAgnostic)
        {
            if (transientErrorCode.ErrorCode.Equals(errorCode))
            {
                //database-agnostic match
                if (result == null)
                {
                    result = transientErrorCode;
                }

                if (Equals(transientErrorCode.DatabaseType, databaseType))
                {
                    //exact match, exit loop and return it
                    break;
                }
            }
        }

        return result;
    }
}