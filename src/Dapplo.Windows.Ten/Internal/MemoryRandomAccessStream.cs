// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Windows.Storage.Streams;

namespace Dapplo.Windows.Ten.Internal
{
    /// <summary>
    /// This is an IRandomAccessStream implementation which uses a MemoryStream
    /// </summary>
	internal sealed class MemoryRandomAccessStream : MemoryStream, IRandomAccessStream
	{
        /// <summary>
        /// Default constructor
        /// </summary>
		public MemoryRandomAccessStream()
		{
		}

        /// <summary>
        /// Constructor where also bytes are already passed
        /// </summary>
        /// <param name="bytes">byte array</param>
		public MemoryRandomAccessStream(byte[] bytes)
		{
			Write(bytes, 0, bytes.Length);
		}

        /// <inheritdoc />
        public IInputStream GetInputStreamAt(ulong position)
		{
			Seek((long)position, SeekOrigin.Begin);

			return this.AsInputStream();
		}

        /// <inheritdoc />
        public IOutputStream GetOutputStreamAt(ulong position)
		{
			Seek((long)position, SeekOrigin.Begin);

			return this.AsOutputStream();
		}

        /// <inheritdoc />
        ulong IRandomAccessStream.Position => (ulong)Position;

        /// <inheritdoc />
        public ulong Size
		{
			get => (ulong)Length;
            set => SetLength((long)value);
        }

        /// <inheritdoc />
        public IRandomAccessStream CloneStream()
		{
			var cloned = new MemoryRandomAccessStream();
			CopyTo(cloned);
			return cloned;
		}

        /// <inheritdoc />
        public void Seek(ulong position)
		{
			Seek((long)position, SeekOrigin.Begin);
		}

        /// <inheritdoc />
        public global::Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			var inputStream = GetInputStreamAt(0);
			return inputStream.ReadAsync(buffer, count, options);
		}

        /// <inheritdoc />
        global::Windows.Foundation.IAsyncOperation<bool> IOutputStream.FlushAsync()
		{
			var outputStream = GetOutputStreamAt(0);
			return outputStream.FlushAsync();
		}

        /// <inheritdoc />
        public global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			var outputStream = GetOutputStreamAt(0);
			return outputStream.WriteAsync(buffer);
		}
	}
}
