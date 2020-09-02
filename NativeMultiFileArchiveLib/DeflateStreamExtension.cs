using System;
using System.IO;

namespace NativeMultiFileArchiveLib
{
    internal static class DeflateStreamExtension
    {
        public static void CopyTo(this Stream source, Stream destination, int bufferSize)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            if (!source.CanRead && !source.CanWrite)
            {
                throw new ObjectDisposedException(nameof(source));
            }

            if (!destination.CanRead && !destination.CanWrite)
            {
                throw new ObjectDisposedException(nameof(bufferSize));
            }

            if (!source.CanRead)
            {
                throw new NotSupportedException();
            }

            if (!destination.CanWrite)
            {
                throw new NotSupportedException();
            }

            source.InternalCopyTo(destination, bufferSize);
        }

        private static void InternalCopyTo(this Stream source, Stream destination, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, read);
            }
        }
    }
}
