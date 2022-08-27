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

namespace MozJpegFileType.Xmp
{
    internal readonly struct ExtendedXmpData
    {
        public ExtendedXmpData(byte[] standardXmpBytes, List<byte[]> extendedXmp)
        {
            if (standardXmpBytes is null)
            {
                throw new ArgumentNullException(nameof(standardXmpBytes));
            }

            if (extendedXmp is null)
            {
                throw new ArgumentNullException(nameof(extendedXmp));
            }

            this.StandardXmpBytes = standardXmpBytes;
            this.ExtendedXmpChunks = extendedXmp;
        }

        public byte[] StandardXmpBytes { get; }

        public List<byte[]> ExtendedXmpChunks { get; }
    }
}
