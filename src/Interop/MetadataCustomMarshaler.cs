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
        private struct NativeExtendedXmpBlock
        {
            public IntPtr data;
            public UIntPtr length;
        }

        // This must be kept in sync with the MetadataParams structure in MozJpegFileTypeIO.h.
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMetadataParams
        {
            public IntPtr exif;
            public UIntPtr exifSize;
            public IntPtr iccProfile;
            public UIntPtr iccProfileSize;
            public IntPtr xmp;
            public UIntPtr xmpSize;
            public IntPtr extendedXmpBlocks;
            public UIntPtr extendedXmpBlockCount;
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

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            unsafe
            {
                if (pNativeData != IntPtr.Zero)
                {
                    NativeMetadataParams* metadata = (NativeMetadataParams*)pNativeData;

                    if (metadata->iccProfile != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(metadata->iccProfile);
                    }

                    if (metadata->exif != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(metadata->exif);
                    }

                    if (metadata->xmp != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(metadata->xmp);

                        if (metadata->extendedXmpBlocks != IntPtr.Zero)
                        {
                            NativeExtendedXmpBlock* blocks = (NativeExtendedXmpBlock*)metadata->extendedXmpBlocks;
                            uint extendedXmpBlockCount = metadata->extendedXmpBlockCount.ToUInt32();

                            for (uint i = 0; i < extendedXmpBlockCount; i++)
                            {
                                Marshal.FreeHGlobal(blocks[i].data);
                            }

                            Marshal.FreeHGlobal((IntPtr)blocks);
                        }
                    }

                    Marshal.FreeHGlobal(pNativeData);
                }
            }
        }

        public int GetNativeDataSize()
        {
            return NativeMetadataParamsSize;
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj == null)
            {
                return IntPtr.Zero;
            }

            MetadataParams metadata = (MetadataParams)ManagedObj;

            IntPtr nativeStructure = Marshal.AllocHGlobal(NativeMetadataParamsSize);

            unsafe
            {
                NativeMetadataParams* nativeMetadata = (NativeMetadataParams*)nativeStructure;

                if (metadata.exif != null && metadata.exif.Length > 0)
                {
                    nativeMetadata->exif = Marshal.AllocHGlobal(metadata.exif.Length);
                    Marshal.Copy(metadata.exif, 0, nativeMetadata->exif, metadata.exif.Length);
                    nativeMetadata->exifSize = new UIntPtr((uint)metadata.exif.Length);
                }
                else
                {
                    nativeMetadata->exif = IntPtr.Zero;
                    nativeMetadata->exifSize = UIntPtr.Zero;
                }

                if (metadata.iccProfile != null && metadata.iccProfile.Length > 0)
                {
                    nativeMetadata->iccProfile = Marshal.AllocHGlobal(metadata.iccProfile.Length);
                    Marshal.Copy(metadata.iccProfile, 0, nativeMetadata->iccProfile, metadata.iccProfile.Length);
                    nativeMetadata->iccProfileSize = new UIntPtr((uint)metadata.iccProfile.Length);
                }
                else
                {
                    nativeMetadata->iccProfile = IntPtr.Zero;
                    nativeMetadata->iccProfileSize = UIntPtr.Zero;
                }

                if (metadata.standardXmp != null && metadata.standardXmp.Length > 0)
                {
                    nativeMetadata->xmp = Marshal.AllocHGlobal(metadata.standardXmp.Length);
                    Marshal.Copy(metadata.standardXmp, 0, nativeMetadata->xmp, metadata.standardXmp.Length);
                    nativeMetadata->xmpSize = new UIntPtr((uint)metadata.standardXmp.Length);

                    int extendedXmpBlockCount = metadata.extendedXmpChunks.Count;

                    if (extendedXmpBlockCount > 0)
                    {
                        IntPtr extendedXmpBlockLength = new IntPtr((long)extendedXmpBlockCount * NativeExtendedXmpBlockSize);
                        nativeMetadata->extendedXmpBlocks = Marshal.AllocHGlobal(extendedXmpBlockLength);
                        nativeMetadata->extendedXmpBlockCount = new UIntPtr((uint)extendedXmpBlockCount);

                        NativeExtendedXmpBlock* blocks = (NativeExtendedXmpBlock*)nativeMetadata->extendedXmpBlocks;

                        for (int i = 0; i < extendedXmpBlockCount; i++)
                        {
                            byte[] chunk = metadata.extendedXmpChunks[i];

                            NativeExtendedXmpBlock* block = &blocks[i];

                            block->data = Marshal.AllocHGlobal(chunk.Length);
                            Marshal.Copy(chunk, 0, block->data, chunk.Length);
                            block->length = new UIntPtr((uint)chunk.Length);
                        }
                    }
                    else
                    {
                        nativeMetadata->extendedXmpBlocks = IntPtr.Zero;
                        nativeMetadata->extendedXmpBlockCount = UIntPtr.Zero;
                    }
                }
                else
                {
                    nativeMetadata->xmp = IntPtr.Zero;
                    nativeMetadata->xmpSize = UIntPtr.Zero;
                    nativeMetadata->extendedXmpBlocks = IntPtr.Zero;
                    nativeMetadata->extendedXmpBlockCount = UIntPtr.Zero;
                }
            }

            return nativeStructure;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return null;
        }
    }
}
