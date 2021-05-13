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

#include "JpegMetadataWriter.h"

namespace
{
    constexpr int App1Marker = JPEG_APP0 + 1;

    void WriteExifBlock(j_compress_ptr cinfo, const uint8_t* data, size_t dataSize)
    {
        jpeg_write_m_header(cinfo, App1Marker, static_cast<unsigned int>(dataSize));

        for (size_t i = 0; i < dataSize; i++)
        {
            jpeg_write_m_byte(cinfo, data[i]);
        }
    }

    void WriteStandardXmpBlock(j_compress_ptr cinfo, const uint8_t* data, size_t dataSize)
    {
        jpeg_write_m_header(cinfo, App1Marker, static_cast<unsigned int>(dataSize));

        for (size_t i = 0; i < dataSize; i++)
        {
            jpeg_write_m_byte(cinfo, data[i]);
        }
    }

    void WriteExtendedXmpBlocks(j_compress_ptr cinfo, const ExtendedXmpBlock* blocks, size_t blockCount)
    {
        for (size_t i = 0; i < blockCount; i++)
        {
            const ExtendedXmpBlock* block = &blocks[i];
            const uint8_t* data = block->data;
            const size_t dataSize = block->length;

            jpeg_write_m_header(cinfo, App1Marker, static_cast<unsigned int>(dataSize));

            for (size_t i = 0; i < dataSize; i++)
            {
                jpeg_write_m_byte(cinfo, data[i]);
            }
        }
    }
}

void WriteMetadata(j_compress_ptr cinfo, const MetadataParams* metadata)
{
    if (metadata->exif != nullptr && metadata->exifSize > 0)
    {
        WriteExifBlock(cinfo, metadata->exif, metadata->exifSize);
    }

    if (metadata->standardXmp != nullptr && metadata->standardXmpSize > 0)
    {
        WriteStandardXmpBlock(cinfo, metadata->standardXmp, metadata->standardXmpSize);

        if (metadata->extendedXmpBlocks != nullptr && metadata->extendedXmpBlockCount > 0)
        {
            WriteExtendedXmpBlocks(cinfo, metadata->extendedXmpBlocks, metadata->extendedXmpBlockCount);
        }
    }

    if (metadata->iccProfile != nullptr && metadata->iccProfileSize > 0)
    {
        jpeg_write_icc_profile(cinfo, metadata->iccProfile, static_cast<unsigned int>(metadata->iccProfileSize));
    }
}
