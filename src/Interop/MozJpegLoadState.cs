////////////////////////////////////////////////////////////////////////
//
// This file is part of pdn-mozjpeg, a FileType plugin for Paint.NET
// that saves JPEG images using the mozjpeg encoder.
//
// Copyright (c) 2021, 2022 Nicholas Hayes
//
// This file is licensed under the MIT License.
// See LICENSE.txt for complete licensing and attribution information.
//
////////////////////////////////////////////////////////////////////////

using MozJpegFileType.Xmp;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Imaging;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace MozJpegFileType.Interop
{
    internal sealed class MozJpegLoadState
    {
        private byte[] exifBytes;
        private byte[] iccProfileBytes;
        private byte[] standardXmpBytes;
        private List<byte[]> extendedXmpBytes;

        private readonly IArrayPoolService arrayPool;

        public MozJpegLoadState(IArrayPoolService arrayPool)
        {
            this.exifBytes = null;
            this.iccProfileBytes = null;
            this.standardXmpBytes = null;
            this.extendedXmpBytes = new List<byte[]>();
            this.ExceptionInfo = null;
            this.Surface = null;
            this.arrayPool = arrayPool;
        }

        public ExceptionDispatchInfo ExceptionInfo { get; private set; }

        public Surface Surface { get; private set; }

        public IntPtr AllocateSurface(int width, int height, out int outStride)
        {
            try
            {
                this.Surface = new Surface(width, height);
                outStride = this.Surface.Stride;

                return this.Surface.Scan0.Pointer;
            }
            catch (Exception ex)
            {
                this.ExceptionInfo = ExceptionDispatchInfo.Capture(ex);
                outStride = 0;
                return IntPtr.Zero;
            }
        }

        public byte[] GetExifBytes() => this.exifBytes;

        public byte[] GetIccProfileBytes() => this.iccProfileBytes;

        public XmpPacket GetXmpPacket()
        {
            if (this.standardXmpBytes is null)
            {
                return null;
            }

            XDocument standardXmp = XmpUtils.TryParseXmpBytes(this.standardXmpBytes);

            if (standardXmp is null)
            {
                return null;
            }

            XmpPacket xmpPacket;

            if (XmpUtils.TryGetExtendedXmpGuid(standardXmp, out string extendedXmpGuid))
            {
                xmpPacket = TryMergeExtendedXmp(standardXmp, extendedXmpGuid);
            }
            else
            {
                xmpPacket = XmpPacket.TryLoad(standardXmp);
            }

            return xmpPacket;
        }

        public bool SetMetadata(IntPtr data, int size, MetadataType type)
        {
            try
            {
                byte[] bytes = new byte[size];

                Marshal.Copy(data, bytes, 0, size);

                switch (type)
                {
                    case MetadataType.Exif:
                        this.exifBytes = bytes;
                        break;
                    case MetadataType.Icc:
                        this.iccProfileBytes = bytes;
                        break;
                    case MetadataType.StandardXmp:
                        this.standardXmpBytes = bytes;
                        break;
                    case MetadataType.ExtendedXmp:
                        this.extendedXmpBytes.Add(bytes);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                this.ExceptionInfo = ExceptionDispatchInfo.Capture(ex);
                return false;
            }

            return true;
        }

        private XmpPacket TryMergeExtendedXmp(XDocument standardXmp, string extendedXmpGuid)
        {
            byte[] mergedPacketBytes = RecombineExtendedXmpChunks(extendedXmpGuid);

            if (mergedPacketBytes is null)
            {
                return null;
            }

            XDocument extendedXmp = XmpUtils.TryParseXmpBytes(mergedPacketBytes);

            if (extendedXmp is null)
            {
                return null;
            }

            XDocument mergedXmp = XmpUtils.MergeXmpPackets(standardXmp, extendedXmp);

            return XmpPacket.TryLoad(mergedXmp);
        }

        private byte[] RecombineExtendedXmpChunks(string extendedXmpGuid)
        {
            byte[] packetBytes = null;

            for (int i = 0; i < this.extendedXmpBytes.Count; i++)
            {
                byte[] extendedXmpData = this.extendedXmpBytes[i];
                ExtendedXMPChunk chunk = XmpUtils.TryParseExtendedXmpData(extendedXmpData, this.arrayPool);

                if (chunk is null)
                {
                    return null;
                }

                try
                {
                    if (!extendedXmpGuid.Equals(chunk.MD5Guid, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    if (packetBytes is null)
                    {
                        packetBytes = new byte[chunk.TotalLength];
                    }
                    else if (packetBytes.LongLength != chunk.TotalLength)
                    {
                        // Invalid ExtendedXMP chunk. Abort.
                        return null;
                    }

                    Array.Copy(chunk.Data.Array, 0, packetBytes, chunk.ChunkOffset, chunk.Data.RequestedLength);
                }
                finally
                {
                    chunk.Dispose();
                }
            }

            return packetBytes;
        }
    }
}
