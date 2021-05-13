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

#pragma once

#include "MozJpegFileTypeIO.h"
#include <stdio.h>
#include <jpeglib.h>
#include <jerror.h>

void WriteMetadata(j_compress_ptr cinfo, const MetadataParams* metadata);
