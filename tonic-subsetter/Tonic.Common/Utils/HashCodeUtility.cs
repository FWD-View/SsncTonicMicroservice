using System;
using System.Runtime.InteropServices;
using Standart.Hash.xxHash;
using Tonic.Common.Extensions;

namespace Tonic.Common.Utils;

public static class HashCodeUtility
{

    // We can take advantage of a struct to convert the into into bytes. The byte fields and int fields will be stored in the same
    //  spot in memory, populating `intval` will populate the byte values as well. (Assumes LittleEndian)
    //  This ends up being faster than BitConverter.GetBytes()
    [StructLayout(LayoutKind.Explicit)]
    private struct IntToByteUnion
    {
        [FieldOffset(0)]
        public Int32 intval;

        [FieldOffset(0)]
        public byte b0;

        [FieldOffset(1)]
        public byte b1;

        [FieldOffset(2)]
        public byte b2;

        [FieldOffset(3)]
        public byte b3;
    }

    // Optimized version for combining just two arguments. Roughly takes half the time than calling params version with two arguments
    public static int GetCompositeHashCode<T1, T2>(T1 item1, T2 item2)
    {
        byte[] bytes = new byte[8];

        var varIntToByteHash = new IntToByteUnion
        {
            intval = item1?.GetHashCode() ?? 0
        };

        bytes[0] = varIntToByteHash.b0;
        bytes[1] = varIntToByteHash.b1;
        bytes[2] = varIntToByteHash.b2;
        bytes[3] = varIntToByteHash.b3;

        varIntToByteHash.intval = item2?.GetHashCode() ?? 0;
        bytes[4] = varIntToByteHash.b0;
        bytes[5] = varIntToByteHash.b1;
        bytes[6] = varIntToByteHash.b2;
        bytes[7] = varIntToByteHash.b3;

        var hash = xxHash32.ComputeHash(bytes, bytes.Length);
        return unchecked((int) hash);
    }

    public static int GetCompositeHashCode(params object?[] items)
        => CombineHashCodes(items, o => o?.GetHashCode() ?? 0);

    public static int GetCompositeDeterministicHashCode(params object?[] items)
        => CombineHashCodes(items, o => o?.GetDeterministicHashCode() ?? 0);

    public static int GetCompositeHashCode<T>(T[] items)
        => CombineHashCodes(items, t => t?.GetHashCode() ?? 0);

    public static int GetCompositeDeterministicHashCode<T>(T[] items)
        => CombineHashCodes(items, t => t?.GetDeterministicHashCode() ?? 0);

    private const int _NumberOfBytesInInteger = 4;
    /// <summary>
    /// Combines the HashCodes of the given list of elements together, using given function to get the hashcode for an
    ///     individual element of the list.
    ///
    ///     Combines the hashcodes together using the xxHash32 algorithm. The algortihm that HashCode.Combine uses and successfully
    ///     passes the SMHasher test suite.
    ///
    /// </summary>
    /// <param name="elements">The list of elements to combine the hash codes of</param>
    /// <param name="getHashCode">The function to get a hash code for an individual item of elements</param>
    /// <typeparam name="T"></typeparam>
    /// <returns>The combined hashcode of all of the elements</returns>
    public static int CombineHashCodes<T>(T[] elements, Func<T, int> getHashCode)
    {
        if (elements.Length == 0) return 0;

        var intToByte = new IntToByteUnion();
        var allHashcodeBytes = new byte[elements.Length * _NumberOfBytesInInteger];
        for (int i = 0; i < elements.Length; i++)
        {
            intToByte.intval = elements[i] == null ? 0 : getHashCode(elements[i]);
            int start = i * _NumberOfBytesInInteger;
            allHashcodeBytes[start] = intToByte.b0;
            allHashcodeBytes[start + 1] = intToByte.b1;
            allHashcodeBytes[start + 2] = intToByte.b2;
            allHashcodeBytes[start + 3] = intToByte.b3;
        }

        var hash = xxHash32.ComputeHash(allHashcodeBytes, allHashcodeBytes.Length);
        return unchecked((int) hash);
    }
}