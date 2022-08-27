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

#pragma once

#include <stdint.h>

typedef bool(__stdcall* ProgressCallback)(int32_t progress);

typedef int32_t(__stdcall* ReadCallback)(void* buffer, int32_t size);

typedef bool(__stdcall* SkipBytesCallback)(int32_t numberOfBytesToSkip);

typedef bool(__stdcall* WriteCallback)(const void* buffer, size_t size);

typedef uint8_t*(__stdcall* AllocateSurfaceCallback)(int32_t width, int32_t height, int32_t* outStride);

enum class MetadataType : int
{
    Exif = 0,
    Icc,
    StandardXmp,
    ExtendedXmp
};

typedef void(__stdcall* SetMetadataCallback)(const void* buffer, int32_t size, MetadataType type);

struct ReadCallbacks
{
    ReadCallback read;
    SkipBytesCallback skipBytes;
    AllocateSurfaceCallback allocateSurface;
    SetMetadataCallback setMetadata;
};

enum class DecodeStatus : int
{
    Ok = 0,
    NullParameter,
    OutOfMemory,
    JpegLibraryError,
    UserCanceled
};

enum class ChromaSubsampling : int
{
    Subsampling420 = 0,
    Subsampling422,
    Subsampling444,
    Subsampling400
};

struct EncodeOptions
{
    int quality;
    ChromaSubsampling chromaSubsampling;
    bool progressive;
};

enum class EncodeStatus : int
{
    Ok = 0,
    NullParameter,
    OutOfMemory,
    JpegLibraryError,
    UserCanceled
};

struct BitmapData
{
    uint8_t* scan0;
    uint32_t width;
    uint32_t height;
    uint32_t stride;
};

struct JpegLibraryErrorInfo
{
    static const size_t maxErrorMessageLength = 255;

    char errorMessage[maxErrorMessageLength + 1];
};

// This must be kept in sync with the NativeExtendedXmpBlock structure in MetadataCustomMarshaler.cs.
struct ExtendedXmpBlock
{
    uint8_t* data;
    size_t length;
};

// This must be kept in sync with the NativeMetadataParams structure in MetadataCustomMarshaler.cs.
struct MetadataParams
{
    uint8_t* exif;
    size_t exifSize;
    uint8_t* iccProfile;
    size_t iccProfileSize;
    uint8_t* standardXmp;
    size_t standardXmpSize;
    ExtendedXmpBlock* extendedXmpBlocks;
    size_t extendedXmpBlockCount;
};

extern "C" __declspec(dllexport) DecodeStatus ReadImage(
    const ReadCallbacks* callbacks,
    JpegLibraryErrorInfo* errorInfo);

extern "C" __declspec(dllexport) EncodeStatus WriteImage(
    const BitmapData* bgraImage,
    const EncodeOptions* options,
    const MetadataParams* metadata,
    JpegLibraryErrorInfo* errorInfo,
    ProgressCallback progressCallback,
    WriteCallback writeCallback);
