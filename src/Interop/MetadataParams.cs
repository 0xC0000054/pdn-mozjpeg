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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MozJpegFileType.Interop
{

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class MetadataParams
    {
        public byte[] iccProfile;
        public byte[] exif;
        public byte[] standardXmp;
        public List<byte[]> extendedXmpChunks;

        public MetadataParams(byte[] exifBytes,
                              byte[] iccProfileBytes,
                              byte[] standardXmpBytes,
                              List<byte[]> extendedXmpChunks)
        {
            if (extendedXmpChunks is null)
            {
                throw new ArgumentNullException(nameof(extendedXmpChunks));
            }

            this.exif = exifBytes;
            this.iccProfile = iccProfileBytes;
            this.standardXmp = standardXmpBytes;
            this.extendedXmpChunks = extendedXmpChunks;
        }
    }
}
