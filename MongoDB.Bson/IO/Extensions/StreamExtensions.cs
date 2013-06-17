/* Copyright 2010-2013 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO.Extensions
{
    /// <summary>
    /// Contains extension methods for Stream.
    /// </summary>
    public static class StreamExtensions
    {
        // private static fields
        private static readonly string[] __asciiStringTable = new string[128];
        private static readonly bool[] __validBsonTypes = new bool[256];

        // static constructor
        static StreamExtensions()
        {
            for (int i = 0; i < __asciiStringTable.Length; ++i)
            {
                __asciiStringTable[i] = new string((char)i, 1);
            }

            foreach (BsonType bsonType in Enum.GetValues(typeof(BsonType)))
            {
                __validBsonTypes[(byte)bsonType] = true;
            }
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

        /// <summary>
        /// Reads a BSON boolean from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A bool.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        public static bool ReadBsonBoolean(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var b = stream.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }

            return (b != 0);
        }

        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A string.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// encoding
        /// </exception>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        public static string ReadBsonCString(this Stream stream, UTF8Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                return bsonStream.ReadBsonCString(encoding);
            }
            else
            {
                var bytes = new List<byte>();
                while (true)
                {
                    var b = stream.ReadByte();
                    if (b == -1)
                    {
                        throw new EndOfStreamException();
                    }
                    else if (b == 0)
                    {
                        break;
                    }
                    else
                    {
                        bytes.Add((byte)b);
                    }
                }
                return DecodeUtf8String(bytes.ToArray(), 0, bytes.Count, encoding);
            }
        }

        /// <summary>
        /// Reads a BSON double from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A double.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static double ReadBsonDouble(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                return bsonStream.ReadBsonDouble();
            }
            else
            {
                var bytes = stream.ReadBytes(8);
                return BitConverter.ToDouble(bytes, 0);
            }
        }

        /// <summary>
        /// Reads a 32-bit BSON integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>An int.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static int ReadBsonInt32(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                return bsonStream.ReadBsonInt32();
            }
            else
            {
                var bytes = stream.ReadBytes(4);
                return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
            }
        }

        /// <summary>
        /// Reads a 64-bit BSON integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A long.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static long ReadBsonInt64(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                return bsonStream.ReadBsonInt64();
            }
            else
            {
                var bytes = stream.ReadBytes(8);
                var lo = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
                var hi = (uint)(bytes[4] | (bytes[5] << 8) | (bytes[6] << 16) | (bytes[7] << 24));
                return (long)(((ulong)hi << 32) | (ulong)lo);
            }
        }

        /// <summary>
        /// Reads a BSON ObjectId from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>An ObjectId.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static ObjectId ReadBsonObjectId(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                return bsonStream.ReadBsonObjectId();
            }
            else
            {
                var bytes = stream.ReadBytes(12);
                return new ObjectId(bytes);
            }
        }

        /// <summary>
        /// Reads a BSON string from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A string.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// encoding
        /// </exception>
        /// <exception cref="System.FormatException">
        /// String is missing null terminator byte.
        /// </exception>
        public static string ReadBsonString(this Stream stream, UTF8Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }
           
            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                return bsonStream.ReadBsonString(encoding);
            }
            else
            {
                var length = stream.ReadBsonInt32();
                if (length < 1)
                {
                    var message = string.Format("Invalid string length: {0}.", length);
                    throw new FormatException(message);
                }

                var bytes = stream.ReadBytes(length); // read the null byte also (included in length)
                if (bytes[length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }

                return DecodeUtf8String(bytes, 0, length - 1, encoding); // don't decode the null byte
            }
        }

        /// <summary>
        /// Reads a BSON type code from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A BsonType.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        /// <exception cref="System.FormatException"></exception>
        public static BsonType ReadBsonType(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            
            var b = stream.ReadByte();
            if (b == -1)
            {
                throw new EndOfStreamException();
            }
            if (!__validBsonTypes[b])
            {
                string message = string.Format("Invalid BsonType: {0}.", b);
                throw new FormatException(message);
            }

            return (BsonType)b;
        }

        /// <summary>
        /// Reads bytes from the stream and stores them in an existing buffer. Throws EndOfStreamException if not enough bytes are available.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset in the buffer at which to start storing the bytes being read.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.ArgumentException">Count cannot be negative.;count</exception>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        public static void ReadBytes(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (count < 0)
            {
                throw new ArgumentException("Count cannot be negative.", "count");
            }

            while (count > 0)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += read;
                count -= read;
            }
        }

        /// <summary>
        /// Reads bytes from the stream. Throws EndOfStreamException if not enough bytes are available.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="count">The count.</param>
        /// <returns>A byte array.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        /// <exception cref="System.ArgumentException">Count cannot be negative.;count</exception>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        public static byte[] ReadBytes(this Stream stream, int count)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (count < 0)
            {
                throw new ArgumentException("Count cannot be negative.", "count");
            }

            var bytes = new byte[count];
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(bytes, offset, count - offset);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += read;
            }

            return bytes;
        }

        /// <summary>
        /// Skips over a BSON CString positioning the stream to just after the terminating null byte.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static void SkipBsonCString(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            
            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                bsonStream.SkipBsonCString();
            }
            else
            {
                while (true)
                {
                    var b = stream.ReadByte();
                    if (b == -1)
                    {
                        throw new EndOfStreamException();
                    }
                    else if (b == 0)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Writes a BSON boolean to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static void WriteBsonBoolean(this Stream stream, bool value)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            stream.WriteByte((byte)(value ? 1 : 0));
        }

        /// <summary>
        /// Writes a BSON CString to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// encoding
        /// </exception>
        /// <exception cref="System.ArgumentException">UTF8 representation cannot contain null bytes when writing a BSON CString.;value</exception>
        public static void WriteBsonCString(this Stream stream, string value, UTF8Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                bsonStream.WriteBsonCString(value, encoding);
            }
            else
            {
                var bytes = encoding.GetBytes(value);
                if (bytes.Contains<byte>(0))
                {
                    throw new ArgumentException("UTF8 representation cannot contain null bytes when writing a BSON CString.", "value");
                }
                stream.Write(bytes, 0, bytes.Length);
                stream.WriteByte(0);
            }
        }

        /// <summary>
        /// Writes a BSON double to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static void WriteBsonDouble(this Stream stream, double value)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                bsonStream.WriteBsonDouble(value);
            }
            else
            {
                var bytes = BitConverter.GetBytes(value);
                stream.Write(bytes, 0, 8);
            }
        }

        /// <summary>
        /// Writes a 32-bit BSON integer to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static void WriteBsonInt32(this Stream stream, int value)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                bsonStream.WriteBsonInt32(value);
            }
            else
            {
                var bytes = new byte[4];
                bytes[0] = (byte)value;
                bytes[1] = (byte)(value >> 8);
                bytes[2] = (byte)(value >> 16);
                bytes[3] = (byte)(value >> 24);
                stream.Write(bytes, 0, 4);
            }
        }

        /// <summary>
        /// Writes a 64-bit BSON integer to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static void WriteBsonInt64(this Stream stream, long value)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                bsonStream.WriteBsonInt64(value);
            }
            else
            {
                var bytes = new byte[8];
                bytes[0] = (byte)value;
                bytes[1] = (byte)(value >> 8);
                bytes[2] = (byte)(value >> 16);
                bytes[3] = (byte)(value >> 24);
                bytes[4] = (byte)(value >> 32);
                bytes[5] = (byte)(value >> 40);
                bytes[6] = (byte)(value >> 48);
                bytes[7] = (byte)(value >> 56);
                stream.Write(bytes, 0, 8);
            }
        }

        /// <summary>
        /// Writes a BSON ObjectId to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static void WriteBsonObjectId(this Stream stream, ObjectId value)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                bsonStream.WriteBsonObjectId(value);
            }
            else
            {
                var bytes = value.ToByteArray();
                stream.Write(bytes, 0, 12);
            }
        }

        /// <summary>
        /// Writes a BSON string to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="System.ArgumentNullException">
        /// stream
        /// or
        /// encoding
        /// </exception>
        public static void WriteBsonString(this Stream stream, string value, UTF8Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            var bsonStream = stream as IBsonStream;
            if (bsonStream != null)
            {
                bsonStream.WriteBsonString(value, encoding);
            }
            else
            {
                var bytes = encoding.GetBytes(value);
                stream.WriteBsonInt32(bytes.Length + 1);
                stream.Write(bytes, 0, bytes.Length);
                stream.WriteByte(0);
            }
        }

        /// <summary>
        /// Writes a BSON type code to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public static void WriteBsonType(this Stream stream, BsonType value)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            stream.WriteByte((byte)value);
        }
   }
}
