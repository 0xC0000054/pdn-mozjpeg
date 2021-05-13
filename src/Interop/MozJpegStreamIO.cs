////////////////////////////////////////////////////////////////////////
//
// This file is part of pdn-mozjpeg, a FileType plugin for Paint.NET
// that saves JPEG images using the mozjpeg encoder.
//
// Copyright (c) 2021 Nicholas Hayes
//
// This file is licensed under the MIT License.
// See LICENSE.txt for complete licensing and attribution information.
//
////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace MozJpegFileType.Interop
{
    internal sealed class MozJpegStreamIO : Disposable
    {
        private const int BufferSize = 4096;

        private IArrayPoolBuffer<byte> buffer;
        private readonly Stream stream;

        public MozJpegStreamIO(Stream stream, PaintDotNet.AppModel.IArrayPoolService arrayPool)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (arrayPool is null)
            {
                throw new ArgumentNullException(nameof(arrayPool));
            }

            this.stream = stream;
            this.buffer = arrayPool.Rent<byte>(BufferSize);
        }

        public ExceptionDispatchInfo ExceptionInfo { get; private set; }

        public int Read(IntPtr data, int maxNumberOfBytesToRead)
        {
            int totalBytesRead = 0;

            if (maxNumberOfBytesToRead > 0)
            {
                try
                {
                    while (totalBytesRead < maxNumberOfBytesToRead)
                    {
                        int bytesToRead = Math.Min(maxNumberOfBytesToRead - totalBytesRead, BufferSize);
                        int bytesRead = this.stream.Read(this.buffer.Array, 0, bytesToRead);

                        if (bytesRead == 0)
                        {
                            break;
                        }

                        Marshal.Copy(this.buffer.Array, 0, data, bytesRead);

                        totalBytesRead += bytesRead;
                    }
                }
                catch (Exception ex)
                {
                    this.ExceptionInfo = ExceptionDispatchInfo.Capture(ex);
                    return -1;
                }
            }

            return totalBytesRead;
        }

        public bool SkipBytes(int numberOfBytesToSkip)
        {
            if (numberOfBytesToSkip > 0)
            {
                try
                {
                    this.stream.Position += numberOfBytesToSkip;
                }
                catch (Exception ex)
                {
                    this.ExceptionInfo = ExceptionDispatchInfo.Capture(ex);
                    return false;
                }
            }

            return true;
        }

        public bool Write(IntPtr data, UIntPtr dataLength)
        {
            ulong count = dataLength.ToUInt64();

            if (count > 0)
            {
                try
                {
                    ulong offset = 0;
                    ulong remaining = count;

                    unsafe
                    {
                        byte* src = (byte*)data;

                        fixed (byte* dest = this.buffer.Array)
                        {
                            while (remaining > 0)
                            {
                                ulong bytesToCopy = Math.Min(remaining, BufferSize);

                                Buffer.MemoryCopy(src + offset, dest, bytesToCopy, bytesToCopy);

                                this.stream.Write(this.buffer.Array, 0, (int)bytesToCopy);

                                offset += bytesToCopy;
                                remaining -= bytesToCopy;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.ExceptionInfo = ExceptionDispatchInfo.Capture(ex);
                    return false;
                }
            }

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }

            base.Dispose(disposing);
        }
    }
}
