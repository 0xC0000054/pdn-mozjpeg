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

using MozJpegFileType.Interop;
using PaintDotNet;
using PaintDotNet.AppModel;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MozJpegFileType
{
    internal static class MozJpegNative
    {
        public static unsafe MozJpegLoadState Load(Stream input, IArrayPoolService arrayPool)
        {
            MozJpegLoadState loadState = new MozJpegLoadState(arrayPool);

            using (MozJpegStreamIO streamIO = new MozJpegStreamIO(input, arrayPool))
            {
                ReadCallbacks callbacks = new ReadCallbacks
                {
                    read = streamIO.Read,
                    skipBytes = streamIO.SkipBytes,
                    allocateSurface = loadState.AllocateSurface,
                    setIccProfile = loadState.SetMetadata
                };

                JpegLibraryErrorInfo errorInfo = new JpegLibraryErrorInfo();
                DecodeStatus status = DecodeStatus.Ok;

                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    status = MozJpeg_X64.ReadImage(callbacks, ref errorInfo);
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    status = MozJpeg_Arm64.ReadImage(callbacks, ref errorInfo);
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    status = MozJpeg_X86.ReadImage(callbacks, ref errorInfo);
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }

                GC.KeepAlive(callbacks);

                if (status != DecodeStatus.Ok)
                {
                    if (status == DecodeStatus.JpegLibraryError)
                    {
                        if (streamIO.ExceptionInfo != null)
                        {
                            streamIO.ExceptionInfo.Throw();
                        }
                        else if (loadState.ExceptionInfo != null)
                        {
                            loadState.ExceptionInfo.Throw();
                        }
                        else
                        {
                            string libraryError = new string(errorInfo.errorMessage);

                            if (string.IsNullOrWhiteSpace(libraryError))
                            {
                                throw new FormatException("An unknown error occurred when reading the image.");
                            }
                            else
                            {
                                throw new FormatException(libraryError);
                            }
                        }
                    }
                    else
                    {
                        switch (status)
                        {
                            case DecodeStatus.NullParameter:
                                throw new ArgumentException("A required ReadImage parameter was null.");
                            case DecodeStatus.OutOfMemory:
                                throw new OutOfMemoryException();
                            case DecodeStatus.UserCanceled:
                                throw new OperationCanceledException();
                            default:
                                throw new FormatException("An unknown error occurred when reading the image.");
                        }
                    }
                }
            }

            return loadState;
        }

        public static unsafe void Save(
            Surface input,
            Stream output,
            int quality,
            ChromaSubsampling chromaSubsampling,
            bool progressive,
            MetadataParams metadata,
            ProgressEventHandler progressEventHandler,
            IArrayPoolService arrayPool)
        {
            BitmapData bitmap = new BitmapData
            {
                scan0 = (byte*)input.Scan0.VoidStar,
                width = (uint)input.Width,
                height = (uint)input.Height,
                stride = (uint)input.Stride
            };

            EncodeOptions encodeOptions = new EncodeOptions
            {
                quality = quality,
                chromaSubsampling = chromaSubsampling,
                progressive = progressive
            };

            using (MozJpegStreamIO streamIO = new MozJpegStreamIO(output, arrayPool))
            {
                WriteCallback writeCallback = streamIO.Write;

                ProgressCallback progressCallback = null;

                if (progressEventHandler != null)
                {
                    progressCallback = new ProgressCallback(delegate (int progress)
                    {
                        try
                        {
                            progressEventHandler.Invoke(null, new ProgressEventArgs(progress, true));
                            return true;
                        }
                        catch (OperationCanceledException)
                        {
                            return false;
                        }
                    });
                }

                EncodeStatus status = EncodeStatus.Ok;
                JpegLibraryErrorInfo errorInfo = new JpegLibraryErrorInfo();

                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    status = MozJpeg_X64.WriteImage(ref bitmap,
                                                    ref encodeOptions,
                                                    metadata,
                                                    ref errorInfo,
                                                    progressCallback,
                                                    writeCallback);
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    status = MozJpeg_Arm64.WriteImage(ref bitmap,
                                                      ref encodeOptions,
                                                      metadata,
                                                      ref errorInfo,
                                                      progressCallback,
                                                      writeCallback);
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    status = MozJpeg_X86.WriteImage(ref bitmap,
                                                    ref encodeOptions,
                                                    metadata,
                                                    ref errorInfo,
                                                    progressCallback,
                                                    writeCallback);
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }


                GC.KeepAlive(progressCallback);
                GC.KeepAlive(writeCallback);
                GC.KeepAlive(metadata);

                if (status != EncodeStatus.Ok)
                {
                    if (status == EncodeStatus.JpegLibraryError)
                    {
                        if (streamIO.ExceptionInfo != null)
                        {
                            streamIO.ExceptionInfo.Throw();
                        }
                        else
                        {
                            string libraryError = new string(errorInfo.errorMessage);

                            if (string.IsNullOrWhiteSpace(libraryError))
                            {
                                throw new FormatException("An unknown error occurred when writing the image.");
                            }
                            else
                            {
                                throw new FormatException(libraryError);
                            }
                        }
                    }
                    else
                    {
                        switch (status)
                        {
                            case EncodeStatus.NullParameter:
                                throw new ArgumentException("A required WriteImage parameter was null.");
                            case EncodeStatus.OutOfMemory:
                                throw new OutOfMemoryException();
                            case EncodeStatus.UserCanceled:
                                throw new OperationCanceledException();
                            default:
                                throw new FormatException("An unknown error occurred when writing the image.");
                        }
                    }

                }
            }
        }
    }
}
