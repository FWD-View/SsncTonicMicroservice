using System;

namespace Tonic.Common.OracleHelper.Enums
{
    /// <summary>
    /// struct mode_t
    /// </summary>
    /// <remarks>
    /// Used by <see cref="LibC.Chmod(string,FileAccessPermissions)"/>
    /// http://permissions-calculator.org/decode/
    /// </remarks>
    [Flags]
    public enum ModeT : uint
    {

        /// <summary>
        /// Set-user-id mode, indicating that the effective user ID of any user executing the file should be made the same as that of the owner of the file.
        /// </summary>
        S_ISUID = 0x0800, // Set user ID on execution
        /// <summary>
        /// Set-group-id mode, indicating that the effective group ID of any user executing the file should be made the same as the group of the file.
        /// </summary>
        S_ISGID = 0x0400, // Set group ID on execution
        /// <summary>
        /// Is sticky bit set.
        /// </summary>
        S_ISVTX = 0x0200, // Save swapped text after use (sticky).
        /// <summary>
        /// Is readable by user (owner).
        /// </summary>
        S_IRUSR = 0x0100, // Read by owner
        /// <summary>
        /// Is writable by user (owner).
        /// </summary>
        S_IWUSR = 0x0080, // Write by owner
        /// <summary>
        /// Is executable for user (owner).
        /// </summary>
        S_IXUSR = 0x0040, // Execute by owner
        /// <summary>
        /// Is readable for group.
        /// </summary>
        S_IRGRP = 0x0020, // Read by group
        /// <summary>
        /// Is writable for group.
        /// </summary>
        S_IWGRP = 0x0010, // Write by group
        /// <summary>
        /// Is executable for group.
        /// </summary>
        S_IXGRP = 0x0008, // Execute by group
        /// <summary>
        /// Is readable for others (everyone).
        /// </summary>
        S_IROTH = 0x0004, // Read by other
        /// <summary>
        /// Is writable for others (everyone).
        /// </summary>
        S_IWOTH = 0x0002, // Write by other
        /// <summary>
        /// Is executable for others (everyone).
        /// </summary>
        S_IXOTH = 0x0001, // Execute by other

        /// <summary>
        /// Group has read, write, and execute permissions.
        /// </summary>
        S_IRWXG = (S_IRGRP | S_IWGRP | S_IXGRP),
        /// <summary>
        /// Owner has read, write, and execute permissions.
        /// </summary>
        S_IRWXU = (S_IRUSR | S_IWUSR | S_IXUSR),
        /// <summary>
        /// Others have read, write, and execute permissions.
        /// </summary>
        S_IRWXO = (S_IROTH | S_IWOTH | S_IXOTH),
        /// <summary>
        /// Allow all user, group, and others read, write, and execute access (0777).
        /// </summary>
        ACCESSPERMS = (S_IRWXU | S_IRWXG | S_IRWXO), // 0777
        /// <summary>
        /// All permissions enabled, including set user ID and set group ID on execution bits (07777).
        /// </summary>
        ALLPERMS = (S_ISUID | S_ISGID | S_ISVTX | S_IRWXU | S_IRWXG | S_IRWXO), // 07777
        /// <summary>
        /// Default file mode: permit read and write access to owner, group, others (0666).
        /// </summary>
        DEFFILEMODE = (S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH), // 0666

        // Device types
        /// <summary>
        /// Bitmask for the file type bitfields.
        /// </summary>
        S_IFMT = 0xF000, // Bits which determine file type
        /// <summary>
        /// Directory.
        /// </summary>
        S_IFDIR = 0x4000, // Directory
        /// <summary>
        /// Character device.
        /// </summary>
        S_IFCHR = 0x2000, // Character device
        /// <summary>
        /// Block device.
        /// </summary>
        S_IFBLK = 0x6000, // Block device
        /// <summary>
        /// Regular file.
        /// </summary>
        S_IFREG = 0x8000, // Regular file
        /// <summary>
        /// First In First Out (FIFO).
        /// </summary>
        S_IFIFO = 0x1000, // FIFO
        /// <summary>
        /// Symbolic link.
        /// </summary>
        S_IFLNK = 0xA000, // Symbolic link
        /// <summary>
        /// Socket.
        /// </summary>
        S_IFSOCK = 0xC000, // Socket
    }
}