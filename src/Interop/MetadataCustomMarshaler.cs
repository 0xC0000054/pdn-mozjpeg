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
    internal sealed class MetadataCustomMarshaler : ICustomMarshaler
    {
        // This must be kept in sync with the ExtendedXmpBlock structure in MozJpegFileTypeIO.h.
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct NativeExtendedXmpBlock
        {
            public void* data;
            public nuint length;
        }

        // This must be kept in sync with the MetadataParams structure in MozJpegFileTypeIO.h.
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct NativeMetadataParams
        {
            public void* exif;
            public nuint exifSize;
            public void* iccProfile;
            public nuint iccProfileSize;
            public void* xmp;
            public nuint xmpSize;
            public NativeExtendedXmpBlock* extendedXmpBlocks;
            public nuint extendedXmpBlockCount;
        }

        private static readonly int NativeExtendedXmpBlockSize = Marshal.SizeOf<NativeExtendedXmpBlock>();
        private static readonly int NativeMetadataParamsSize = Marshal.SizeOf<NativeMetadataParams>();
        private static readonly MetadataCustomMarshaler instance = new MetadataCustomMarshaler();

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Style",
            "IDE0060:Remove unused parameter",
            Justification = "The cookie parameter is required by the ICustomMarshaler API.")]
        public static ICustomMarshaler GetInstance(string cookie)
        {
            return instance;
        }

        private MetadataCustomMarshaler()
        {
        }

        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public unsafe void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData != IntPtr.Zero)
            {
                NativeMetadataParams* metadata = (NativeMetadataParams*)pNativeData;

                if (metadata->iccProfile != null)
                {
                    NativeMemory.Free(metadata->iccProfile);
                }

                if (metadata->exif != null)
                {
                    NativeMemory.Free(metadata->exif);
                }

                if (metadata->xmp != null)
                {
                    NativeMemory.Free(metadata->xmp);

                    if (metadata->extendedXmpBlocks != null)
                    {
                        NativeExtendedXmpBlock* blocks = metadata->extendedXmpBlocks;
                        nuint extendedXmpBlockCount = metadata->extendedXmpBlockCount;

                        for (nuint i = 0; i < extendedXmpBlockCount; i++)
                        {
                            NativeMemory.Free(blocks[i].data);
                        }

                        NativeMemory.Free(blocks);
                    }
                }

                NativeMemory.Free(metadata);
            }
        }

        public int GetNativeDataSize()
        {
            return NativeMetadataParamsSize;
        }

        public unsafe IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj == null)
            {
                return IntPtr.Zero;
            }

            MetadataParams metadata = (MetadataParams)ManagedObj;

            NativeMetadataParams* nativeMetadata = (NativeMetadataParams*)NativeMemory.Alloc((uint)NativeMetadataParamsSize);

            if (metadata.exif != null && metadata.exif.Length > 0)
            {
                nativeMetadata->exif = NativeMemory.Alloc((uint)metadata.exif.Length);
                metadata.exif.AsSpan().CopyTo(new Span<byte>(nativeMetadata->exif, metadata.exif.Length));
                nativeMetadata->exifSize = (uint)metadata.exif.Length;
            }
            else
            {
                nativeMetadata->exif = null;
                nativeMetadata->exifSize = 0;
            }

            if (metadata.iccProfile != null && metadata.iccProfile.Length > 0)
            {
                nativeMetadata->iccProfile = NativeMemory.Alloc((uint)metadata.iccProfile.Length);
                metadata.iccProfile.AsSpan().CopyTo(new Span<byte>(nativeMetadata->iccProfile, metadata.iccProfile.Length));
                nativeMetadata->iccProfileSize = (uint)metadata.iccProfile.Length;
            }
            else
            {
                nativeMetadata->iccProfile = null;
                nativeMetadata->iccProfileSize = 0;
            }

            if (metadata.standardXmp != null && metadata.standardXmp.Length > 0)
            {
                nativeMetadata->xmp = NativeMemory.Alloc((uint)metadata.standardXmp.Length);
                metadata.standardXmp.AsSpan().CopyTo(new Span<byte>(nativeMetadata->xmp, metadata.standardXmp.Length));
                nativeMetadata->xmpSize = (uint)metadata.standardXmp.Length;

                int extendedXmpBlockCount = metadata.extendedXmpChunks.Count;

                if (extendedXmpBlockCount > 0)
                {
                    nativeMetadata->extendedXmpBlocks = (NativeExtendedXmpBlock*)NativeMemory.Alloc((uint)extendedXmpBlockCount,
                                                                                                    (uint)NativeExtendedXmpBlockSize);
                    nativeMetadata->extendedXmpBlockCount = (uint)extendedXmpBlockCount;

                    NativeExtendedXmpBlock* blocks = nativeMetadata->extendedXmpBlocks;

                    for (int i = 0; i < extendedXmpBlockCount; i++)
                    {
                        ReadOnlySpan<byte> chunk = metadata.extendedXmpChunks[i];

                        NativeExtendedXmpBlock* block = &blocks[i];

                        block->data = NativeMemory.Alloc((uint)chunk.Length);
                        chunk.CopyTo(new Span<byte>(block->data, chunk.Length));
                        block->length = (uint)chunk.Length;
                    }
                }
                else
                {
                    nativeMetadata->extendedXmpBlocks = null;
                    nativeMetadata->extendedXmpBlockCount = 0;
                }
            }
            else
            {
                nativeMetadata->xmp = null;
                nativeMetadata->xmpSize = 0;
                nativeMetadata->extendedXmpBlocks = null;
                nativeMetadata->extendedXmpBlockCount = 0;
            }

            return (IntPtr)nativeMetadata;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return null;
        }
    }
}
