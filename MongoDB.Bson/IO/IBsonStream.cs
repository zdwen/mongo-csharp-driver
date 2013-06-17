using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents specialized performance critial reading and writing methods for BSON values. Classes
    /// that implement Stream can choose to also implement this interface to improve performance when
    /// reading and writing BSON values.
    /// </summary>
    public interface IBsonStream
    {
        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A string.</returns>
        string ReadBsonCString(UTF8Encoding encoding);

        /// <summary>
        /// Reads a BSON double from the stream.
        /// </summary>
        /// <returns>A double.</returns>
        double ReadBsonDouble();

        /// <summary>
        /// Reads a 32-bit BSON integer from the stream.
        /// </summary>
        /// <returns>An int.</returns>
        int ReadBsonInt32();

        /// <summary>
        /// Reads a 64-bit BSON integer from the stream.
        /// </summary>
        /// <returns>A long.</returns>
        long ReadBsonInt64();

        /// <summary>
        /// Reads a BSON ObjectId from the stream.
        /// </summary>
        /// <returns>An ObjectId.</returns>
        ObjectId ReadBsonObjectId();

        /// <summary>
        /// Reads a BSON string from the stream.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A string.</returns>
        string ReadBsonString(UTF8Encoding encoding);

        /// <summary>
        /// Skips over a BSON CString leaving the stream positioned just after the terminating null byte.
        /// </summary>
        void SkipBsonCString();

        /// <summary>
        /// Writes a BSON CString to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        void WriteBsonCString(string value, UTF8Encoding encoding);

        /// <summary>
        /// Writes a BSON double to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteBsonDouble(double value);

        /// <summary>
        /// Writes a 32-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteBsonInt32(int value);

        /// <summary>
        /// Writes a 64-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteBsonInt64(long value);

        /// <summary>
        /// Writes a BSON ObjectId to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteBsonObjectId(ObjectId value);

        /// <summary>
        /// Writes a BSON string to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        void WriteBsonString(string value, UTF8Encoding encoding);
    }
}
