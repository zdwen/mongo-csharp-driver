using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.IO.Extensions;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a Stream backed by an IByteBuffer. Similar to MemoryStream but backed by an IByteBuffer
    /// instead of a byte array and also implements the IBsonStream interface for higher performance BSON I/O.
    /// </summary>
    public class ByteBufferStream : Stream, IBsonStream
    {
        // private fields
        private readonly IByteBuffer _byteBuffer;
        private readonly bool _ownsByteBuffer;
        private bool _disposed;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteBufferStream"/> class.
        /// </summary>
        /// <param name="byteBuffer">The byte buffer.</param>
        public ByteBufferStream(IByteBuffer byteBuffer)
            : this(byteBuffer, ownsByteBuffer: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteBufferStream"/> class.
        /// </summary>
        /// <param name="byteBuffer">The byte buffer.</param>
        /// <param name="ownsByteBuffer">Whether the stream owns the byteBuffer and should Dispose it when done.</param>
        public ByteBufferStream(IByteBuffer byteBuffer, bool ownsByteBuffer)
        {
            _byteBuffer = byteBuffer;
            _ownsByteBuffer = ownsByteBuffer;
        }

        // public properties
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value that determines whether the current stream can time out.
        /// </summary>
        /// <returns>A value that determines whether the current stream can time out.</returns>
        public override bool CanTimeout
        {
            get { return false; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite
        {
            get { return !_byteBuffer.IsReadOnly; }
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get { return _byteBuffer.Length; }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        public override long Position
        {
            get { return _byteBuffer.Position; }
            set { _byteBuffer.Position = (int)value; }
        }

        // public methods
        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            // do nothing
        }

        /// <summary>
        /// Loads this memory stream from the contents of another stream.
        /// </summary>
        /// <param name="stream">The stream to load this one from.</param>
        /// <param name="count">The number of bytes to load.</param>
        public void LoadFrom(Stream stream, int count)
        {
            _byteBuffer.LoadFrom(stream, count);
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            _byteBuffer.ReadBytes(buffer, offset, count);
            return count;
        }

        /// <summary>
        /// Reads a BSON CString from the stream.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>
        /// A string.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">encoding</exception>
        /// <exception cref="System.FormatException">CString is missing terminating null byte.</exception>
        public string ReadBsonCString(UTF8Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            var nullPosition = _byteBuffer.FindNullByte();
            if (nullPosition == -1)
            {
                throw new FormatException("CString is missing terminating null byte.");
            }

            var length = nullPosition - _byteBuffer.Position + 1; // read null byte also
            var segment = _byteBuffer.ReadBackingBytes(length);
            if (segment.Count >= length)
            {
                return StreamExtensions.DecodeUtf8String(segment.Array, segment.Offset, length - 1, encoding); // don't decode null byte
            }
            else
            {
                var bytes = _byteBuffer.ReadBytes(length);
                return StreamExtensions.DecodeUtf8String(bytes, 0, length - 1, encoding); // don't decode null byte
            }
        }

        /// <summary>
        /// Reads a BSON double from the stream.
        /// </summary>
        /// <returns>
        /// A double.
        /// </returns>
        public double ReadBsonDouble()
        {
            var segment = _byteBuffer.ReadBackingBytes(8);
            if (segment.Count >= 8)
            {
                return BitConverter.ToDouble(segment.Array, segment.Offset);
            }
            else
            {
                var bytes = _byteBuffer.ReadBytes(8);
                return BitConverter.ToDouble(bytes, 0);
            }
        }

        /// <summary>
        /// Reads a 32-bit BSON integer from the stream.
        /// </summary>
        /// <returns>
        /// An int.
        /// </returns>
        public int ReadBsonInt32()
        {
            var segment = _byteBuffer.ReadBackingBytes(4);
            if (segment.Count >= 4)
            {
                var bytes = segment.Array;
                var offset = segment.Offset;
                return bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24);
            }
            else
            {
                var bytes = _byteBuffer.ReadBytes(4);
                return bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
            }
        }

        /// <summary>
        /// Reads a 64-bit BSON integer from the stream.
        /// </summary>
        /// <returns>
        /// A long.
        /// </returns>
        public long ReadBsonInt64()
        {
            var segment = _byteBuffer.ReadBackingBytes(8);
            if (segment.Count >= 8)
            {
                var bytes = segment.Array;
                var offset = segment.Offset;
                var lo = (uint)(bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));
                var hi = (uint)(bytes[offset + 4] | (bytes[offset + 5] << 8) | (bytes[offset + 6] << 16) | (bytes[offset + 7] << 24));
                return (long)(((ulong)hi << 32) | (ulong)lo);
            }
            else
            {
                var bytes = _byteBuffer.ReadBytes(8);
                var lo = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
                var hi = (uint)(bytes[4] | (bytes[5] << 8) | (bytes[6] << 16) | (bytes[7] << 24));
                return (long)(((ulong)hi << 32) | (ulong)lo);
            }
        }

        /// <summary>
        /// Reads a BSON ObjectId from the stream.
        /// </summary>
        /// <returns>
        /// An ObjectId.
        /// </returns>
        public ObjectId ReadBsonObjectId()
        {
            var segment = _byteBuffer.ReadBackingBytes(12);
            if (segment.Count >= 12)
            {
                return new ObjectId(segment.Array, segment.Offset);
            }
            else
            {
                var bytes = _byteBuffer.ReadBytes(12);
                return new ObjectId(bytes);
            }
        }

        /// <summary>
        /// Reads a BSON string from the stream.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>
        /// A string.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">encoding</exception>
        /// <exception cref="System.FormatException">
        /// String is missing terminating null byte.
        /// or
        /// String is missing terminating null byte.
        /// </exception>
        public string ReadBsonString(UTF8Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            var length = ReadBsonInt32();
            if (length <= 0)
            {
                var message = string.Format("Invalid string length: {0}.", length);
                throw new FormatException(message);
            }

            var segment = _byteBuffer.ReadBackingBytes(length);
            if (segment.Count >= length)
            {
                if (segment.Array[segment.Offset + length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }
                return StreamExtensions.DecodeUtf8String(segment.Array, segment.Offset, length - 1, encoding);
            }
            else
            {
                var bytes = _byteBuffer.ReadBytes(length);
                if (bytes[length - 1] != 0)
                {
                    throw new FormatException("String is missing terminating null byte.");
                }
                return StreamExtensions.DecodeUtf8String(bytes, 0, length - 1, encoding);
            }
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="System.ArgumentException">origin</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long position;
            switch (origin)
            {
                case SeekOrigin.Begin: position = offset; break;
                case SeekOrigin.Current: position = _byteBuffer.Position + offset; break;
                case SeekOrigin.End: position = _byteBuffer.Length + offset; break;
                default: throw new ArgumentException("origin");
            }
            _byteBuffer.Position = (int)position;
            return position;
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            _byteBuffer.Length = (int)value;
        }

        /// <summary>
        /// Skips over a BSON CString leaving the stream positioned just after the terminating null byte.
        /// </summary>
        /// <exception cref="System.IO.EndOfStreamException"></exception>
        public void SkipBsonCString()
        {
            var nullPosition = _byteBuffer.FindNullByte();
            if (nullPosition == -1)
            {
                throw new EndOfStreamException();
            }
            _byteBuffer.Position = nullPosition + 1;
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _byteBuffer.WriteBytes(buffer, offset, count);
        }

        /// <summary>
        /// Writes a BSON CString to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="System.ArgumentException">
        /// UTF8 representation of a CString cannot contain null bytes.
        /// or
        /// UTF8 representation of a CString cannot contain null bytes.
        /// </exception>
        public void WriteBsonCString(string value, UTF8Encoding encoding)
        {
            var maxLength = encoding.GetMaxByteCount(value.Length) + 1;
            var segment = _byteBuffer.WriteBackingBytes(maxLength);
            if (segment.Count >= maxLength)
            {
                var length = encoding.GetBytes(value, 0, value.Length, segment.Array, segment.Offset);
                if (Array.IndexOf<byte>(segment.Array, 0, segment.Offset, length) != -1)
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }
                segment.Array[segment.Offset + length] = 0;
                _byteBuffer.Position += length + 1;
            }
            else
            {
                var bytes = encoding.GetBytes(value);
                if (bytes.Contains<byte>(0))
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }

                _byteBuffer.WriteBytes(bytes, 0, bytes.Length);
                _byteBuffer.WriteByte(0);
            }
        }

        /// <summary>
        /// Writes a BSON double to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBsonDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);

            var segment = _byteBuffer.WriteBackingBytes(8);
            if (segment.Count >= 8)
            {
                segment.Array[segment.Offset] = bytes[0];
                segment.Array[segment.Offset + 1] = bytes[1];
                segment.Array[segment.Offset + 2] = bytes[2];
                segment.Array[segment.Offset + 3] = bytes[3];
                segment.Array[segment.Offset + 4] = bytes[4];
                segment.Array[segment.Offset + 5] = bytes[5];
                segment.Array[segment.Offset + 6] = bytes[6];
                segment.Array[segment.Offset + 7] = bytes[7];
                _byteBuffer.Position += 8;
            }
            else
            {
                _byteBuffer.WriteBytes(bytes, 0, 8);
            }
        }

        /// <summary>
        /// Writes a 32-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBsonInt32(int value)
        {
            var segment = _byteBuffer.WriteBackingBytes(4);
            if (segment.Count >= 4)
            {
                segment.Array[segment.Offset] = (byte)value;
                segment.Array[segment.Offset + 1] = (byte)(value >> 8);
                segment.Array[segment.Offset + 2] = (byte)(value >> 16);
                segment.Array[segment.Offset + 3] = (byte)(value >> 24);
                _byteBuffer.Position += 4;
            }
            else
            {
                var bytes = new byte[4];
                bytes[0] = (byte)value;
                bytes[1] = (byte)(value >> 8);
                bytes[2] = (byte)(value >> 16);
                bytes[3] = (byte)(value >> 24);
                _byteBuffer.WriteBytes(bytes, 0, 4);
            }
        }

        /// <summary>
        /// Writes a 64-bit BSON integer to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBsonInt64(long value)
        {
            var segment = _byteBuffer.WriteBackingBytes(8);
            if (segment.Count >= 8)
            {
                segment.Array[segment.Offset] = (byte)value;
                segment.Array[segment.Offset + 1] = (byte)(value >> 8);
                segment.Array[segment.Offset + 2] = (byte)(value >> 16);
                segment.Array[segment.Offset + 3] = (byte)(value >> 24);
                segment.Array[segment.Offset + 4] = (byte)(value >> 32);
                segment.Array[segment.Offset + 5] = (byte)(value >> 40);
                segment.Array[segment.Offset + 6] = (byte)(value >> 48);
                segment.Array[segment.Offset + 7] = (byte)(value >> 56);
                _byteBuffer.Position += 8;
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
                _byteBuffer.WriteBytes(bytes, 0, 8);
            }
        }

        /// <summary>
        /// Writes a BSON ObjectId to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBsonObjectId(ObjectId value)
        {

            var segment = _byteBuffer.WriteBackingBytes(12);
            if (segment.Count >= 12)
            {
                value.GetBytes(segment.Array, segment.Offset);
                _byteBuffer.Position += 12;
            }
            else
            {
                var bytes = value.ToByteArray();
                _byteBuffer.WriteBytes(bytes, 0, 12);
            }
        }

        /// <summary>
        /// Writes a BSON string to the stream.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="System.ArgumentException">
        /// UTF8 representation of a CString cannot contain null bytes.
        /// or
        /// UTF8 representation of a CString cannot contain null bytes.
        /// </exception>
        public void WriteBsonString(string value, UTF8Encoding encoding)
        {
            var maxLength = encoding.GetMaxByteCount(value.Length) + 5;
            var segment = _byteBuffer.WriteBackingBytes(maxLength);
            if (segment.Count >= maxLength)
            {
                var length = encoding.GetBytes(value, 0, value.Length, segment.Array, segment.Offset + 4);
                if (Array.IndexOf<byte>(segment.Array, 0, segment.Offset, length) != -1)
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }
                var lengthPlusOne = length + 1;
                segment.Array[segment.Offset] = (byte)lengthPlusOne;
                segment.Array[segment.Offset + 1] = (byte)(lengthPlusOne >> 8);
                segment.Array[segment.Offset + 2] = (byte)(lengthPlusOne >> 16);
                segment.Array[segment.Offset + 3] = (byte)(lengthPlusOne >> 24);
                segment.Array[segment.Offset + 4 + length] = 0;
                _byteBuffer.Position += length + 5;
            }
            else
            {
                var bytes = encoding.GetBytes(value);
                if (bytes.Contains<byte>(0))
                {
                    throw new ArgumentException("UTF8 representation of a CString cannot contain null bytes.");
                }

                _byteBuffer.WriteBytes(bytes, 0, bytes.Length);
                _byteBuffer.WriteByte(0);
            }
        }

        // protected methods
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_ownsByteBuffer)
                {
                    _byteBuffer.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
