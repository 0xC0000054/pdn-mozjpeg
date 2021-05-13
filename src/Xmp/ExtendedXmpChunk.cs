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

namespace MozJpegFileType.Xmp
{
    internal sealed class ExtendedXMPChunk : Disposable
    {
        private IArrayPoolBuffer<byte> data;

        public ExtendedXMPChunk(string md5Guid, uint totalLength, uint chunkOffset, IArrayPoolBuffer<byte> data)
        {
            this.MD5Guid = md5Guid;
            this.TotalLength = totalLength;
            this.ChunkOffset = chunkOffset;
            this.data = data;
        }

        public string MD5Guid { get; }

        public uint TotalLength { get; }

        public uint ChunkOffset { get; }

        public IArrayPoolBuffer<byte> Data => this.data;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposableUtil.Free(ref this.data);
            }

            base.Dispose(disposing);
        }
    }
}
