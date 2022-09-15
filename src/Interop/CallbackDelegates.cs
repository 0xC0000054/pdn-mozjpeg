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
using System.Runtime.InteropServices;

namespace MozJpegFileType.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal delegate bool ProgressCallback(int progress);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int ReadCallback(IntPtr data, int maxNumberOfBytesToRead);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal delegate bool SkipBytesCallback(int numberOfBytesToSkip);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal delegate bool WriteCallback(IntPtr data, UIntPtr dataSize);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate IntPtr AllocateSurfaceCallback(int width, int height, out int stride);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal delegate bool SetMetadataCallback(IntPtr data, int size, MetadataType type);
}
