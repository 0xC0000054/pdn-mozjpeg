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

using System.Runtime.InteropServices;

namespace MozJpegFileType.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class ReadCallbacks
    {
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ReadCallback read;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public SkipBytesCallback skipBytes;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public AllocateSurfaceCallback allocateSurface;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public SetMetadataCallback setIccProfile;
    }
}
