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

namespace MozJpegFileType.Exif
{
    internal static class MetadataHelpers
    {
        internal static byte[] EncodeLong(uint value)
        {
            return new byte[]
            {
                (byte)(value & 0xff),
                (byte)(value >> 8),
                (byte)(value >> 16),
                (byte)(value >> 24)
            };
        }

        internal static byte[] EncodeShort(ushort value)
        {
            return new byte[]
            {
                (byte)(value & 0xff),
                (byte)(value >> 8)
            };
        }

        internal static bool TryDecodeShort(MetadataEntry entry, out ushort value)
        {
            if (entry.Type != TagDataType.Short || entry.LengthInBytes != 2)
            {
                value = 0;
                return false;
            }

            byte[] data = entry.GetDataReadOnly();

            value = (ushort)(data[0] | (data[1] << 8));

            return true;
        }
    }
}
