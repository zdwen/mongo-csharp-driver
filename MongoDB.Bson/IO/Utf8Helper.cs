using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a class that has some helper methods for decoding UTF8 strings.
    /// </summary>
    public static class Utf8Helper
    {
        // private static fields
        private static readonly string[] __asciiStringTable = new string[128];
        private static readonly UTF8Encoding __strictUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // static constructor
        static Utf8Helper()
        {
            for (int i = 0; i < __asciiStringTable.Length; ++i)
            {
                __asciiStringTable[i] = new string((char)i, 1);
            }
        }

        // public static properties
        /// <summary>
        /// Gets a strict UTF8 encoding.
        /// </summary>
        /// <value>
        /// A strict UTF8 encoding.
        /// </value>
        public static UTF8Encoding StrictUtf8Encoding
        {
            get { return __strictUtf8Encoding; }
        }

        // public static methods
        /// <summary>
        /// Decodes a UTF8 string.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>The decoded string.</returns>
        public static string DecodeUtf8String(byte[] bytes, int index, int count, UTF8Encoding encoding)
        {
            switch (count)
            {
                // special case empty strings
                case 0:
                    return "";

                // special case single character strings
                case 1:
                    var byte1 = (int)bytes[index];
                    if (byte1 < __asciiStringTable.Length)
                    {
                        return __asciiStringTable[byte1];
                    }
                    break;
            }

            return encoding.GetString(bytes, index, count);
        }
    }
}
