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

namespace MozJpegFileType
{
    internal enum ChromaSubsampling
    {
        /// <summary>
        /// 4:2:0 (best compression)
        /// </summary>
        Subsampling420,

        /// <summary>
        /// 4:2:2
        /// </summary>
        Subsampling422,

        /// <summary>
        /// 4:4:4 (best quality)
        /// </summary>
        Subsampling444,

        /// <summary>
        /// YUV 4:0:0
        /// </summary>
        /// <remarks>
        /// Used internally for gray-scale images, not shown to the user.
        /// </remarks>
        Subsampling400
    }
}
