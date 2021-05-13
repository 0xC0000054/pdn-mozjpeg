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

using System.Runtime.InteropServices;

namespace MozJpegFileType.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct EncodeOptions
    {
        public int quality;
        public ChromaSubsampling chromaSubsampling;
        [MarshalAs(UnmanagedType.U1)]
        public bool progressive;
    }
}
