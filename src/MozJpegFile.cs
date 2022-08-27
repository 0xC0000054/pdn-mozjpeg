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

using MozJpegFileType.Exif;
using MozJpegFileType.Interop;
using MozJpegFileType.Xmp;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Collections;
using PaintDotNet.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MozJpegFileType
{
    internal static class MozJpegFile
    {
        public static Document Load(Stream input, IArrayPoolService arrayPool)
        {
            MozJpegLoadState loadState = MozJpegNative.Load(input, arrayPool);

            Surface surface = loadState.Surface;

            ExifValueCollection exifValues = GetExifValues(loadState, arrayPool);

            if (exifValues != null)
            {
                MetadataEntry orientation = exifValues.GetAndRemoveValue(MetadataKeys.Image.Orientation);

                if (orientation != null)
                {
                    ApplyExifOrientationTransform(orientation, ref surface);
                }
            }

            Document doc = new Document(surface.Width, surface.Height);

            doc.Layers.Add(Layer.CreateBackgroundLayer(surface, takeOwnership: true));

            AddMetadataToDocument(doc, loadState, exifValues);

            return doc;
        }

        public static unsafe void Save(
            Document input,
            Stream output,
            Surface scratchSurface,
            int quality,
            ChromaSubsampling chromaSubsampling,
            bool progressive,
            ProgressEventHandler progressCallback,
            IArrayPoolService arrayPool)
        {
            using (RenderArgs args = new RenderArgs(scratchSurface))
            {
                input.Render(args, true);
            }

            if (IsGrayscale(scratchSurface))
            {
                // Chroma sub-sampling 4:0:0 is always used for gray-scale images because it
                // produces the smallest file size with no quality loss.
                chromaSubsampling = ChromaSubsampling.Subsampling400;
            }

            MetadataParams metadata = CreateMozJpegMetadata(input);

            MozJpegNative.Save(scratchSurface,
                               output,
                               quality,
                               chromaSubsampling,
                               progressive,
                               metadata,
                               progressCallback,
                               arrayPool);
        }

        private static void AddMetadataToDocument(Document document,
                                                  MozJpegLoadState loadState,
                                                  ExifValueCollection exifValues)
        {
            if (exifValues != null)
            {
                exifValues.Remove(MetadataKeys.Image.InterColorProfile);

                foreach (MetadataEntry entry in exifValues)
                {
                    document.Metadata.AddExifPropertyItem(entry.CreateExifPropertyItem());
                }
            }

            byte[] iccProfileBytes = loadState.GetIccProfileBytes();

            if (iccProfileBytes != null)
            {
                ExifPropertyKey interColorProfile = ExifPropertyKeys.Image.InterColorProfile;

                document.Metadata.AddExifPropertyItem(interColorProfile.Path.Section,
                                                 interColorProfile.Path.TagID,
                                                 new ExifValue(ExifValueType.Undefined,
                                                               iccProfileBytes));
            }

            XmpPacket xmpPacket = loadState.GetXmpPacket();

            if (xmpPacket != null)
            {
                document.Metadata.SetXmpPacket(xmpPacket);
            }
        }

        private static void ApplyExifOrientationTransform(MetadataEntry orientation, ref Surface surface)
        {
            if (MetadataHelpers.TryDecodeShort(orientation, out ushort exifValue))
            {
                if (exifValue >= TiffConstants.Orientation.TopLeft && exifValue <= TiffConstants.Orientation.LeftBottom)
                {
                    switch (exifValue)
                    {
                        case TiffConstants.Orientation.TopLeft:
                            // Do nothing
                            break;
                        case TiffConstants.Orientation.TopRight:
                            // Flip horizontally.
                            ImageTransform.FlipHorizontal(surface);
                            break;
                        case TiffConstants.Orientation.BottomRight:
                            // Rotate 180 degrees.
                            ImageTransform.Rotate180(surface);
                            break;
                        case TiffConstants.Orientation.BottomLeft:
                            // Flip vertically.
                            ImageTransform.FlipVertical(surface);
                            break;
                        case TiffConstants.Orientation.LeftTop:
                            // Rotate 90 degrees clockwise and flip horizontally.
                            ImageTransform.Rotate90CCW(ref surface);
                            ImageTransform.FlipHorizontal(surface);
                            break;
                        case TiffConstants.Orientation.RightTop:
                            // Rotate 90 degrees clockwise.
                            ImageTransform.Rotate90CCW(ref surface);
                            break;
                        case TiffConstants.Orientation.RightBottom:
                            // Rotate 270 degrees clockwise and flip horizontally.
                            ImageTransform.Rotate270CCW(ref surface);
                            ImageTransform.FlipHorizontal(surface);
                            break;
                        case TiffConstants.Orientation.LeftBottom:
                            // Rotate 270 degrees clockwise.
                            ImageTransform.Rotate270CCW(ref surface);
                            break;
                    }
                }
            }
        }

        private static MetadataParams CreateMozJpegMetadata(Document doc)
        {
            byte[] exifBytes = null;
            byte[] iccProfileBytes = null;
            byte[] standardXmpBytes = null;
            List<byte[]> extendedXmpChunks = new List<byte[]>();

            Dictionary<MetadataKey, MetadataEntry> exifMetadata = GetExifMetadataFromDocument(doc);

            if (exifMetadata != null)
            {
                Exif.ExifColorSpace exifColorSpace = Exif.ExifColorSpace.Srgb;

                MetadataKey iccProfileKey = MetadataKeys.Image.InterColorProfile;

                if (exifMetadata.TryGetValue(iccProfileKey, out MetadataEntry iccProfileItem))
                {
                    iccProfileBytes = iccProfileItem.GetData();
                    exifMetadata.Remove(iccProfileKey);
                    exifColorSpace = Exif.ExifColorSpace.Uncalibrated;
                }

                exifBytes = new ExifWriter(doc, exifMetadata, exifColorSpace).CreateExifApp1Payload();
            }

            XmpPacket xmpPacket = doc.Metadata.TryGetXmpPacket();
            if (xmpPacket != null)
            {
                const int MaxStandardXmpPacketLength = 65504;

                string packetAsString = xmpPacket.ToString(XmpPacketWrapperType.ReadOnly);

                byte[] xmpPacketXmlUtf8 = Encoding.UTF8.GetBytes(packetAsString);

                if (xmpPacketXmlUtf8.Length <= MaxStandardXmpPacketLength)
                {
                    standardXmpBytes = XmpUtils.AddSignatureToStandardXmpPacket(xmpPacketXmlUtf8);
                }
                else
                {
                    ExtendedXmpData data = XmpUtils.SplitXmpPacketIntoExtendedXmp(xmpPacketXmlUtf8);

                    standardXmpBytes = data.StandardXmpBytes;
                    extendedXmpChunks = data.ExtendedXmpChunks;
                }
            }

            return new MetadataParams(exifBytes, iccProfileBytes, standardXmpBytes, extendedXmpChunks);
        }

        private static Dictionary<MetadataKey, MetadataEntry> GetExifMetadataFromDocument(Document doc)
        {
            Dictionary<MetadataKey, MetadataEntry> items = null;

            Metadata metadata = doc.Metadata;

            ExifPropertyItem[] exifProperties = metadata.GetExifPropertyItems();

            if (exifProperties.Length > 0)
            {
                items = new Dictionary<MetadataKey, MetadataEntry>(exifProperties.Length);

                foreach (ExifPropertyItem property in exifProperties)
                {
                    MetadataSection section;
                    switch (property.Path.Section)
                    {
                        case ExifSection.Image:
                            section = MetadataSection.Image;
                            break;
                        case ExifSection.Photo:
                            section = MetadataSection.Exif;
                            break;
                        case ExifSection.Interop:
                            section = MetadataSection.Interop;
                            break;
                        case ExifSection.GpsInfo:
                            section = MetadataSection.Gps;
                            break;
                        default:
                            throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                              "Unexpected {0} type: {1}",
                                                                              nameof(ExifSection),
                                                                              (int)property.Path.Section));
                    }

                    MetadataKey metadataKey = new MetadataKey(section, property.Path.TagID);

                    if (!items.ContainsKey(metadataKey))
                    {
                        byte[] clonedData = property.Value.Data.ToArrayEx();

                        items.Add(metadataKey, new MetadataEntry(metadataKey, (TagDataType)property.Value.Type, clonedData));
                    }
                }
            }

            return items;
        }

        private static ExifValueCollection GetExifValues(MozJpegLoadState loadState, IArrayPoolService arrayPool)
        {
            ExifValueCollection exifValues = null;

            byte[] exifBytes = loadState.GetExifBytes();

            if (exifBytes != null)
            {
                exifValues = ExifParser.Parse(exifBytes, arrayPool);
            }

            return exifValues;
        }

        private static unsafe bool IsGrayscale(Surface surface)
        {
            for (int y = 0; y < surface.Height; y++)
            {
                ColorBgra* ptr = surface.GetRowAddressUnchecked(y);
                ColorBgra* ptrEnd = ptr + surface.Width;

                while (ptr < ptrEnd)
                {
                    if (!(ptr->B == ptr->G && ptr->G == ptr->R))
                    {
                        return false;
                    }

                    ptr++;
                }
            }

            return true;
        }
    }
}
