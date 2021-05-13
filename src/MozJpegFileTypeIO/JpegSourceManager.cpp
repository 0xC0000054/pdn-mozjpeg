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

#include "JpegSourceManager.h"

namespace
{
    constexpr int32_t ReadContextBufferSize = 4096;

    struct JpegReadContext
    {
        jpeg_source_mgr mgr;

        ReadCallback read;
        SkipBytesCallback skipBytes;
        JOCTET buffer[ReadContextBufferSize];
        bool startOfFile;
    };

    void init_source(j_decompress_ptr cinfo)
    {
        JpegReadContext* ctx = reinterpret_cast<JpegReadContext*>(cinfo->src);

        ctx->startOfFile = true;
    }

    boolean fill_input_buffer(j_decompress_ptr cinfo)
    {
        JpegReadContext* ctx = reinterpret_cast<JpegReadContext*>(cinfo->src);

        int32_t bytesRead = ctx->read(ctx->buffer, ReadContextBufferSize);

        if (bytesRead == 0) // End of file
        {
            if (ctx->startOfFile)
            {
                ERREXIT(cinfo, JERR_EMPTY_IMAGE);
            }

            // Insert a fake end of image marker.
            ctx->buffer[0] = 0xFF;
            ctx->buffer[1] = JPEG_EOI;
            bytesRead = 2;
        }
        else if (bytesRead == -1) // Other file read errors
        {
            ERREXIT(cinfo, JERR_FILE_READ);
        }

        ctx->mgr.next_input_byte = ctx->buffer;
        ctx->mgr.bytes_in_buffer = bytesRead;
        ctx->startOfFile = false;

        return true;
    }

    void skip_input_data(j_decompress_ptr cinfo, long num_bytes)
    {
        if (num_bytes > 0)
        {
            if (static_cast<size_t>(num_bytes) > cinfo->src->bytes_in_buffer)
            {
                JpegReadContext* ctx = reinterpret_cast<JpegReadContext*>(cinfo->src);

                if (!ctx->skipBytes(num_bytes))
                {
                    ERREXIT(cinfo, JERR_FILE_READ);
                }

                // Force the buffer to be refilled.
                ctx->mgr.next_input_byte = nullptr;
                ctx->mgr.bytes_in_buffer = 0;
            }
            else
            {
                cinfo->src->next_input_byte += num_bytes;
                cinfo->src->bytes_in_buffer -= num_bytes;
            }
        }
    }

    void term_source(j_decompress_ptr cinfo)
    {
        // Nothing to do.
    }
}

void InitializeSourceManager(j_decompress_ptr cinfo, const ReadCallbacks* readCallbacks)
{
    if (cinfo->src == nullptr)
    {
        cinfo->src = static_cast<jpeg_source_mgr*>((*cinfo->mem->alloc_small)(
            reinterpret_cast<j_common_ptr>(cinfo),
            JPOOL_PERMANENT,
            sizeof(JpegReadContext)));
    }
    else if (cinfo->src->init_source != init_source)
    {
        // The destination manager was not created by this function.

        ERREXIT(cinfo, JERR_BUFFER_SIZE);
    }

    JpegReadContext* ctx = reinterpret_cast<JpegReadContext*>(cinfo->src);

    ctx->mgr.init_source = init_source;
    ctx->mgr.fill_input_buffer = fill_input_buffer;
    ctx->mgr.skip_input_data = skip_input_data;
    ctx->mgr.resync_to_restart = jpeg_resync_to_restart;
    ctx->mgr.term_source = term_source;
    ctx->read = readCallbacks->read;
    ctx->skipBytes = readCallbacks->skipBytes;

    ctx->mgr.next_input_byte = nullptr;
    ctx->mgr.bytes_in_buffer = 0;
}
