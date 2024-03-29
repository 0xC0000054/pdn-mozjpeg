﻿////////////////////////////////////////////////////////////////////////
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

namespace MozJpegFileType.Exif
{
    internal enum TagDataType : ushort
    {
        Byte = 1,
        Ascii = 2,
        Short = 3,
        Long = 4,
        Rational = 5,
        SByte = 6,
        Undefined = 7,
        SShort = 8,
        SLong = 9,
        SRational = 10,
        Float = 11,
        Double = 12,
        IFD = 13
    }
}
