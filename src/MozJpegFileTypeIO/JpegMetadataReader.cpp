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

#include "JpegMetadataReader.h"
#include <stdlib.h>
#include <limits>

namespace
{
    void ReadApp1Blocks(j_decompress_ptr cinfo, const ReadCallbacks* callbacks)
    {
        constexpr int App1Marker = JPEG_APP0 + 1;

        constexpr const char* MainExifSignature = "Exif\0\0";
        constexpr const char* AlternateExifSignature = "Exif\0\xFF";
        constexpr unsigned int ExifSignatureLength = 6;

        constexpr const char* StandardXmpSignature = "http://ns.adobe.com/xap/1.0/\0";
        constexpr unsigned int StandardXmpSignatureLength = 29;

        constexpr const char* ExtendedXmpSignature = "http://ns.adobe.com/xmp/extension/\0";
        constexpr unsigned int ExtendedXmpSignatureLength = 35;

        bool setExif = false;
        bool setStandardXmp = false;

        for (jpeg_saved_marker_ptr marker = cinfo->marker_list; marker != nullptr; marker = marker->next)
        {
            if (marker->marker == App1Marker)
            {
                if (marker->data_length > ExifSignatureLength &&
                    memcmp(marker->data, MainExifSignature, ExifSignatureLength) == 0 ||
                    memcmp(marker->data, AlternateExifSignature, ExifSignatureLength) == 0)
                {
                    if (setExif)
                    {
                        continue;
                    }

                    unsigned int exifLength = marker->data_length - ExifSignatureLength;

                    if (exifLength > 0 &&
                        exifLength <= static_cast<unsigned int>(std::numeric_limits<int32_t>::max()))
                    {
                        callbacks->setMetadata(
                            marker->data + ExifSignatureLength,
                            static_cast<int32_t>(exifLength),
                            MetadataType::Exif);
                        setExif = true;
                    }
                }
                else if (marker->data_length > StandardXmpSignatureLength &&
                         memcmp(marker->data, StandardXmpSignature, StandardXmpSignatureLength) == 0)
                {
                    if (setStandardXmp)
                    {
                        continue;
                    }

                    unsigned int xmpLength = marker->data_length - StandardXmpSignatureLength;

                    if (xmpLength > 0 &&
                        xmpLength <= static_cast<unsigned int>(std::numeric_limits<int32_t>::max()))
                    {
                        callbacks->setMetadata(
                            marker->data + StandardXmpSignatureLength,
                            static_cast<int32_t>(xmpLength),
                            MetadataType::StandardXmp);
                        setStandardXmp = true;
                    }
                }
                else if (marker->data_length > ExtendedXmpSignatureLength &&
                         memcmp(marker->data, ExtendedXmpSignature, ExtendedXmpSignatureLength) == 0)
                {
                    unsigned int extendedXmpLength = marker->data_length - ExtendedXmpSignatureLength;

                    if (extendedXmpLength > 0 &&
                        extendedXmpLength <= static_cast<unsigned int>(std::numeric_limits<int32_t>::max()))
                    {
                        callbacks->setMetadata(
                            marker->data + ExtendedXmpSignatureLength,
                            static_cast<int32_t>(extendedXmpLength),
                            MetadataType::ExtendedXmp);
                    }
                }
            }
        }
    }

    void ReadIccProfile(j_decompress_ptr cinfo, const ReadCallbacks* callbacks)
    {
        JOCTET* iccProfile;
        unsigned int iccProfileSize;

        if (jpeg_read_icc_profile(cinfo, &iccProfile, &iccProfileSize))
        {
            if (iccProfileSize > 0 &&
                iccProfileSize <= static_cast<unsigned int>(std::numeric_limits<int32_t>::max()))
            {
                callbacks->setMetadata(iccProfile, static_cast<int32_t>(iccProfileSize), MetadataType::Icc);
            }

            free(iccProfile);
        }
    }
}

void ReadMetadata(j_decompress_ptr cinfo, const ReadCallbacks* callbacks)
{
    ReadApp1Blocks(cinfo, callbacks);
    ReadIccProfile(cinfo, callbacks);
}
