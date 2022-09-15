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

#include "MozJpegFileTypeIO.h"
#include "JpegDestiniationManager.h"
#include "JpegMetadataReader.h"
#include "JpegMetadataWriter.h"
#include "JpegSourceManager.h"
#include <stdlib.h>
#include <memory>
#include <new>
#include <stdio.h>
#include <jpeglib.h>
#include <jerror.h>
#include <setjmp.h>

namespace
{
    struct JpegErrorContext
    {
        jpeg_error_mgr mgr;

        char messageBuffer[JMSG_LENGTH_MAX];
        jmp_buf setjmpBuffer;
    };

    void error_exit(j_common_ptr cinfo)
    {
        JpegErrorContext* ctx = reinterpret_cast<JpegErrorContext*>(cinfo->err);

        switch (ctx->mgr.msg_code)
        {
        case JERR_FILE_READ:
            strcpy_s(ctx->messageBuffer, "File read error.");
            break;
        case JERR_FILE_WRITE:
            strcpy_s(ctx->messageBuffer, "File write error.");
            break;
        default:
            ctx->mgr.format_message(reinterpret_cast<j_common_ptr>(cinfo), ctx->messageBuffer);
            break;
        }

        longjmp(ctx->setjmpBuffer, 1);
    }

    void HandleErrorMessage(const JpegErrorContext& ctx, JpegLibraryErrorInfo* info)
    {
        const size_t errorMessageLength = strlen(ctx.messageBuffer);

        if (errorMessageLength > 0 && errorMessageLength <= JpegLibraryErrorInfo::maxErrorMessageLength)
        {
            strncpy_s(info->errorMessage, ctx.messageBuffer, errorMessageLength);
        }
    }

    struct ColorBgra
    {
        uint8_t b;
        uint8_t g;
        uint8_t r;
        uint8_t a;
    };
}

DecodeStatus ReadImage(
    const ReadCallbacks* callbacks,
    JpegLibraryErrorInfo* errorInfo)
{
    if (callbacks == nullptr || errorInfo == nullptr)
    {
        return DecodeStatus::NullParameter;
    }

    JpegErrorContext errorContext;
    jpeg_decompress_struct cinfo;

    cinfo.err = jpeg_std_error(&errorContext.mgr);
    cinfo.err->error_exit = error_exit;
    memset(errorContext.messageBuffer, 0, _countof(errorContext.messageBuffer));

    if (setjmp(errorContext.setjmpBuffer))
    {
        // This block will be jumped to if the JPEG error_exit method is called.
        jpeg_destroy_decompress(&cinfo);

        HandleErrorMessage(errorContext, errorInfo);
        return DecodeStatus::JpegLibraryError;
    }

    jpeg_create_decompress(&cinfo);

    InitializeSourceManager(&cinfo, callbacks);

    // Save the EXIF and/or XMP data.
    jpeg_save_markers(&cinfo, JPEG_APP0 + 1, 0xFFFF);
    // Save the ICC profile.
    jpeg_save_markers(&cinfo, JPEG_APP0 + 2, 0xFFFF);

    jpeg_read_header(&cinfo, true);

    cinfo.out_color_space = JCS_RGB;

    jpeg_calc_output_dimensions(&cinfo);

    if (cinfo.output_width > static_cast<JDIMENSION>(std::numeric_limits<int32_t>::max()) ||
        cinfo.output_height > static_cast<JDIMENSION>(std::numeric_limits<int32_t>::max()))
    {
        jpeg_destroy_decompress(&cinfo);

        return DecodeStatus::OutOfMemory;
    }


    int32_t outputImageStride = 0;

    uint8_t* outputImageScan0 = callbacks->allocateSurface(cinfo.output_width, cinfo.output_height, &outputImageStride);

    if (outputImageScan0 == nullptr)
    {
        jpeg_destroy_decompress(&cinfo);

        return DecodeStatus::CallbackError;
    }

    const size_t jpegRowBufferSize = static_cast<size_t>(cinfo.output_width) * static_cast<size_t>(cinfo.output_components);

    if (jpegRowBufferSize > std::numeric_limits<JDIMENSION>::max())
    {
        jpeg_destroy_decompress(&cinfo);

        return DecodeStatus::OutOfMemory;
    }

    JSAMPARRAY scanlines = cinfo.mem->alloc_sarray(
        reinterpret_cast<j_common_ptr>(&cinfo),
        JPOOL_IMAGE,
        static_cast<JDIMENSION>(jpegRowBufferSize),
        1);

    jpeg_start_decompress(&cinfo);

    size_t destRow = 0;

    while (cinfo.output_scanline < cinfo.output_height)
    {
        jpeg_read_scanlines(&cinfo, scanlines, 1);

        const JSAMPROW srcRow = scanlines[0];

        ColorBgra* dest = reinterpret_cast<ColorBgra*>(outputImageScan0 + (destRow * outputImageStride));

        size_t srcIndex = 0;

        for (JDIMENSION x = 0; x < cinfo.output_width; x++)
        {
            dest->r = srcRow[srcIndex];
            dest->g = srcRow[srcIndex + 1];
            dest->b = srcRow[srcIndex + 2];
            dest->a = 255;

            srcIndex += 3;
            dest++;
        }

        destRow++;
    }

    DecodeStatus status = ReadMetadata(&cinfo, callbacks);

    jpeg_finish_decompress(&cinfo);
    jpeg_destroy_decompress(&cinfo);

    return status;
}

EncodeStatus WriteImage(
    const BitmapData* bgraImage,
    const EncodeOptions* options,
    const MetadataParams* metadata,
    JpegLibraryErrorInfo* errorInfo,
    ProgressCallback progressCallback,
    WriteCallback writeCallback)
{
    if (bgraImage == nullptr || options == nullptr || errorInfo == nullptr|| writeCallback == nullptr)
    {
        return EncodeStatus::NullParameter;
    }

    JpegErrorContext errorContext;
    jpeg_compress_struct cinfo;

    cinfo.err = jpeg_std_error(&errorContext.mgr);
    cinfo.err->error_exit = error_exit;
    memset(errorContext.messageBuffer, 0, _countof(errorContext.messageBuffer));

    if (setjmp(errorContext.setjmpBuffer))
    {
        // This block will be jumped to if the JPEG error_exit method is called.
        jpeg_destroy_compress(&cinfo);

        HandleErrorMessage(errorContext, errorInfo);
        return EncodeStatus::JpegLibraryError;
    }

    jpeg_create_compress(&cinfo);

    InitializeDestinationManager(&cinfo, writeCallback);

    const bool isGrayscale = options->chromaSubsampling == ChromaSubsampling::Subsampling400;

    cinfo.image_width = bgraImage->width;
    cinfo.image_height = bgraImage->height;
    cinfo.input_components = isGrayscale ? 1 : 3;
#pragma warning(suppress: 26812) // Suppress C26812: Prefer 'enum class' over 'enum'.
    cinfo.in_color_space = isGrayscale ? JCS_GRAYSCALE : JCS_RGB;

    jpeg_set_defaults(&cinfo);

    jpeg_set_quality(&cinfo, options->quality, !options->progressive);
    cinfo.optimize_coding = true;

    if (options->progressive)
    {
        jpeg_simple_progression(&cinfo);
    }

    if (isGrayscale)
    {
        cinfo.comp_info[0].h_samp_factor = 1;
        cinfo.comp_info[0].v_samp_factor = 1;
    }
    else
    {
        switch (options->chromaSubsampling)
        {

        case ChromaSubsampling::Subsampling420:
            cinfo.comp_info[0].h_samp_factor = 2;
            cinfo.comp_info[0].v_samp_factor = 2;
            cinfo.comp_info[1].h_samp_factor = 1;
            cinfo.comp_info[1].v_samp_factor = 1;
            cinfo.comp_info[2].h_samp_factor = 1;
            cinfo.comp_info[2].v_samp_factor = 1;
            break;
        case ChromaSubsampling::Subsampling422:
            cinfo.comp_info[0].h_samp_factor = 2;
            cinfo.comp_info[0].v_samp_factor = 1;
            cinfo.comp_info[1].h_samp_factor = 1;
            cinfo.comp_info[1].v_samp_factor = 1;
            cinfo.comp_info[2].h_samp_factor = 1;
            cinfo.comp_info[2].v_samp_factor = 1;
            break;
        case ChromaSubsampling::Subsampling444:
            cinfo.comp_info[0].h_samp_factor = 1;
            cinfo.comp_info[0].v_samp_factor = 1;
            cinfo.comp_info[1].h_samp_factor = 1;
            cinfo.comp_info[1].v_samp_factor = 1;
            cinfo.comp_info[2].h_samp_factor = 1;
            cinfo.comp_info[2].v_samp_factor = 1;
            break;
        }
    }

    const size_t jpegRowBufferSize = static_cast<size_t>(cinfo.image_width) * static_cast<size_t>(cinfo.input_components);

    if (jpegRowBufferSize > std::numeric_limits<JDIMENSION>::max())
    {
        jpeg_destroy_compress(&cinfo);

        return EncodeStatus::OutOfMemory;
    }

    JSAMPARRAY rowPtr = cinfo.mem->alloc_sarray(
        reinterpret_cast<j_common_ptr>(&cinfo),
        JPOOL_IMAGE,
        static_cast<JDIMENSION>(jpegRowBufferSize),
        1);

    jpeg_start_compress(&cinfo, true);

    WriteMetadata(&cinfo, metadata);

    int32_t currentProgressPercentage = -1;

    while (cinfo.next_scanline < cinfo.image_height)
    {
        if (progressCallback != nullptr)
        {
            double progressPercentage = (static_cast<double>(cinfo.next_scanline) / static_cast<double>(cinfo.image_height)) * 100.0;
            int32_t roundedPercentage = static_cast<int32_t>(round(progressPercentage));

            if (currentProgressPercentage != roundedPercentage)
            {
                currentProgressPercentage = roundedPercentage;

                if (!progressCallback(currentProgressPercentage))
                {
                    jpeg_destroy_compress(&cinfo);

                    return EncodeStatus::UserCanceled;
                }
            }
        }

        const ColorBgra* srcRow = reinterpret_cast<const ColorBgra*>(bgraImage->scan0 + (static_cast<uint64_t>(cinfo.next_scanline) * bgraImage->stride));
        JSAMPROW dstRow = rowPtr[0];

        for (uint32_t x = 0; x < bgraImage->width; x++)
        {
            const ColorBgra pixel = srcRow[x];

            switch (cinfo.input_components)
            {
            case 1:
                dstRow[x] = pixel.r;
                break;
            case 3:
                int index = x * 3;
                dstRow[index] = pixel.r;
                dstRow[index + 1] = pixel.g;
                dstRow[index + 2] = pixel.b;
                break;
            }
        }

        jpeg_write_scanlines(&cinfo, rowPtr, 1);
    }

    jpeg_finish_compress(&cinfo);
    jpeg_destroy_compress(&cinfo);

    return EncodeStatus::Ok;
}
