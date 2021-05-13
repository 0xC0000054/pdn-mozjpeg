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

#include "MozJpegFileTypeIO.h"
#include "JpegDestiniationManager.h"

namespace
{
    constexpr size_t WriteContextBufferSize = 4096;

    struct JpegWriteContext
    {
        jpeg_destination_mgr mgr;

        WriteCallback write;
        JOCTET buffer[WriteContextBufferSize];
    };

    void init_destination(j_compress_ptr cinfo)
    {
        JpegWriteContext* ctx = reinterpret_cast<JpegWriteContext*>(cinfo->dest);

        ctx->mgr.next_output_byte = ctx->buffer;
        ctx->mgr.free_in_buffer = WriteContextBufferSize;
    }

    boolean empty_output_buffer(j_compress_ptr cinfo)
    {
        JpegWriteContext* ctx = reinterpret_cast<JpegWriteContext*>(cinfo->dest);

        if (!ctx->write(ctx->buffer, WriteContextBufferSize))
        {
            ERREXIT(cinfo, JERR_FILE_WRITE);
        }

        ctx->mgr.next_output_byte = ctx->buffer;
        ctx->mgr.free_in_buffer = WriteContextBufferSize;

        return true;
    }

    void term_destination(j_compress_ptr cinfo)
    {
        JpegWriteContext* ctx = reinterpret_cast<JpegWriteContext*>(cinfo->dest);
        size_t remaining = WriteContextBufferSize - ctx->mgr.free_in_buffer;

        if (remaining > 0)
        {
            if (!ctx->write(ctx->buffer, remaining))
            {
                ERREXIT(cinfo, JERR_FILE_WRITE);
            }
        }
    }
}

void InitializeDestinationManager(j_compress_ptr cinfo, WriteCallback writeCallback)
{
    if (cinfo->dest == nullptr)
    {
        cinfo->dest = static_cast<jpeg_destination_mgr*>((*cinfo->mem->alloc_small)(
            reinterpret_cast<j_common_ptr>(cinfo),
            JPOOL_PERMANENT,
            sizeof(JpegWriteContext)));
    }
    else if (cinfo->dest->init_destination != init_destination)
    {
        // The destination manager was not created by this function.

        ERREXIT(cinfo, JERR_BUFFER_SIZE);
    }

    JpegWriteContext* ctx = reinterpret_cast<JpegWriteContext*>(cinfo->dest);

    ctx->mgr.init_destination = init_destination;
    ctx->mgr.empty_output_buffer = empty_output_buffer;
    ctx->mgr.term_destination = term_destination;
    ctx->write = writeCallback;
}
