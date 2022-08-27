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
    internal static class MozJpeg_Arm64
    {
        private const string DllName = "MozJpegFileTypeIO_ARM64.dll";

        [DllImport(DllName)]
        internal static extern unsafe DecodeStatus ReadImage(
           ReadCallbacks callbacks,
           ref JpegLibraryErrorInfo errorInfo);

        [DllImport(DllName)]
        internal static extern unsafe EncodeStatus WriteImage(
            [In] ref BitmapData bitmapData,
            [In] ref EncodeOptions encodeOptions,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MetadataCustomMarshaler))] MetadataParams metadata,
            ref JpegLibraryErrorInfo errorInfo,
            [MarshalAs(UnmanagedType.FunctionPtr)] ProgressCallback progressCallback,
            [MarshalAs(UnmanagedType.FunctionPtr)] WriteCallback writeCallback);
    }
}
