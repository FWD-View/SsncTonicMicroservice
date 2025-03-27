using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Tonic.Common.OracleHelper.ErrorCodes;

/// <summary>
/// Oracle error codes that have special detection / handling in the codebase
/// </summary>
/// <remarks>this does not require assembly reference to any database drivers</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class OracleErrorCodes
{
    [Description("Error Code Not Mapped")]
    public const int Unmapped = TransientErrorCodes.Unmapped;

    /// <summary>
    /// Mapped to <see cref="TransientSqlStates.C00.SuccessfulCompletion"/>
    /// </summary>
    [Description("ORA-00000: normal, successful completion")]
    public const int SuccessfulCompletion = 0;

    /// <summary>
    /// Error codes for which `IsTransient` is true
    /// </summary>
    public static class Transient
    {
        public static class Timeout
        {
            [Description("ORA-12170: TNS: Connect timeout occurred")]
            public const int Connection = 12170;

            [Description("ORA-12525: TNS:listener has not received client's request in time allowed")]
            public const int ClientRequest = 12525;

            [Description("ORA-12608: TNS: Send timeout occurred")]
            public const int Send = 12608;

            [Description("ORA-12609: TNS: Receive timeout occurred")]
            public const int Receive = 12609;

            [Description("ORA-03136: inbound connection timed out")]
            public const int InboundConnection = 03136;
        }

        /// <summary>
        /// Mapped to <see cref="TransientSqlStates.C08.ConnectionDoesNotExist"/>
        /// </summary>
        [Description("ORA-03114: not connected to ORACLE")]
        public const int ConnectionDoesNotExist = 03114;

        /// <summary>
        /// Mapped to <see cref="TransientSqlStates.C08.ConnectionFailure"/>
        /// </summary>
        public static class ConnectionFailure
        {
            [Description("ORA-03113: end-of-file on communication channel")]
            public const int EndOfFile = 03113;

            [Description("ORA-03135: connection lost contact")]
            public const int ConnectionLost = 03135;
        }

        /// <summary>
        /// Mapped to <see cref="TransientSqlStates.C04.DeadlockDetected"/>
        /// </summary>
        public static class DeadlockDetected
        {
            [Description("ORA-04020: deadlock detected while trying to lock object")]
            public const int LockObject = 04020;

            [Description("ORA-00060: deadlock detected while waiting for resource")]
            public const int Resource = 00060;

            [Description("ORA-00104: deadlock detected; all public servers blocked waiting for resources")]
            public const int ServersBlocked = 00104;

            [Description("ORA-02049: timeout: distributed transaction waiting for lock (treat as deadlock)")]
            public const int DistributedTransactionLockTimeout = 02049;

            [Description("ORA-03170: deadlocked on readable physical standby (undo segment string)")]
            public const int UndoSegmentString = 03170;

            [Description("ORA-18013: timed out while waiting for resource (treat as deadlock)")]
            public const int ResourceTimeout = 18013;

            [Description("ORA-18014: deadlock detected while waiting for resource")]
            public const int Resource2 = 18014;

            [Description("ORA-32703: deadlock detected")]
            public const int Deadlock = 32703;
        }

        /// <summary>
        /// Mapped to <see cref="TransientSqlStates.C53.InsufficientResources"/>
        /// </summary>
        [Description("ORA-12510: TNS:database temporarily lacks resources to handle the request")]
        public const int InsufficientResources = 12510;

        /// <summary>
        /// Mapped to <see cref="TransientSqlStates.C08.SqlClientUnableToEstablishSqlConnection"/>
        /// </summary>
        [Description("ORA-12511: TNS:service handler found but it is not accepting connections")]
        public const int SqlClientUnableToEstablishSqlConnection = 12511;

        /// <summary>
        /// Mapped to <see cref="TransientSqlStates.C53.OutOfMemory"/>
        /// </summary>
        public static class OutOfMemory
        {
            [Description("ORA-02100: PCC: out of memory (i.e., could not allocate)")]
            public const int CouldNotAllocate = 02100;

            [Description("ORA-02758: Allocation of internal array failed")]
            public const int InternalArray = 02758;

            [Description("ORA-02787: Unable to allocate memory for segment list")]
            public const int SegmentList = 02787;
        }

        /// <summary>
        /// Mapped to <see cref="TransientSqlStates.C53.TooManyConnections"/>
        /// </summary>
        public static class TooManyConnections
        {
            [Description("ORA-06113: NETTCP: Too many connections")]
            public const int TooMany = 06113;

            [Description("ORA-06143: NETTCP: maximum connections exceeded")]
            public const int MaximumExceeded = 06143;

            [Description("ORA-06308: IPA: No more connections available")]
            public const int NoMoreAvailable = 06308;

            [Description("ORA-12223: TNS: internal limit restriction exceeded")]
            public const int InternalLimitExceeded = 12223;
        }

        /// <summary>
        /// Mapped to <see cref="TransientSqlStates.C55.LockNotAvailable"/>
        /// </summary>
        [Description("ORA-04021: timeout occurred while waiting to lock object")]
        public const int LockNotAvailable = 04021;
    }

    public static class DataPump
    {
        [Description("ORA-39002: invalid operation")]
        public const int InvalidOperation = 39002;

        [Description("ORA-39001: invalid argument value")]
        public const int InvalidArgument = 39001;

        [Description("ORA-39038: Object path %s not supported for TABLE jobs")]
        public const int ObjectPathNotSupportedForTableJobs = 39038;
    }

    [Description("ORA-00955: name is already used by an existing object")]
    public const int NameAlreadyInUse = 00955;

    [Description("ORA-00001: unique constraint (string.string) violated")]
    public const int UniqueConstraintViolation = 00001;

    [Description("ORA-01017: invalid username/password; logon denied")]
    public const int InvalidUsernameOrPassword = 01017;

    [Description("ORA-39171: Job is experiencing a resumable wait.")]
    public const int JobResumableWait = 39171;

    [Description("ORA-00866: invalid or non-existent SQL ID")]
    public const int InvalidSqlId = 00866;

    [Description("ORA-01031: insufficient privileges")]
    public const int InsufficientPrivileges = 01031;

    [Description("ORA-01039: insufficient privileges on underlying objects of the view")]
    public const int InsufficientPrivilegesOnUnderlyingObjectsOfView = 01039;

    [Description("ORA-00942: table or view does not exist")]
    public const int TableOrViewDoesNotExist = 00942;

    [Description("ORA-00904: string: invalid identifier")]
    public const int InvalidIdentifier = 00904;

    [Description("ORA-01013: User requested cancel of current operation.")]
    public const int UserRequestedCancel = 01013;

    private static readonly Regex _oracleErrorRegex =
        new Regex(@".*(ORA-[0-9]*).*", RegexOptions.Compiled);

    public static bool IsMatch(string errorMessage) => _oracleErrorRegex.IsMatch(errorMessage);
}