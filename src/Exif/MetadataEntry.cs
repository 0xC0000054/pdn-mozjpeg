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

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MozJpegFileType.Exif
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    internal sealed class MetadataEntry
        : IEquatable<MetadataEntry>
    {
        private readonly byte[] data;

        public MetadataEntry(MetadataKey key, TagDataType type, byte[] data)
            : this(key.Section, key.TagId, type, data)
        {
        }

        public MetadataEntry(MetadataSection section, ushort tagId, TagDataType type, byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            this.Section = section;
            this.TagId = tagId;
            this.Type = type;
            this.data = (byte[])data.Clone();
        }

        public int LengthInBytes => this.data.Length;

        public MetadataSection Section { get; }

        public ushort TagId { get; }

        public TagDataType Type { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return string.Format("{0}, Tag# {1} (0x{1:X}), {2}", this.Section, this.TagId, this.Type);
            }
        }

        public PaintDotNet.Imaging.ExifPropertyItem CreateExifPropertyItem()
        {
            PaintDotNet.Imaging.ExifSection exifSection;
            switch (this.Section)
            {
                case MetadataSection.Image:
                    exifSection = PaintDotNet.Imaging.ExifSection.Image;
                    break;
                case MetadataSection.Exif:
                    exifSection = PaintDotNet.Imaging.ExifSection.Photo;
                    break;
                case MetadataSection.Gps:
                    exifSection = PaintDotNet.Imaging.ExifSection.GpsInfo;
                    break;
                case MetadataSection.Interop:
                    exifSection = PaintDotNet.Imaging.ExifSection.Interop;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                      "Unexpected {0} type: {1}",
                                                                      nameof(MetadataSection),
                                                                      (int)this.Section));
            }

            return new PaintDotNet.Imaging.ExifPropertyItem(exifSection,
                                                            this.TagId,
                                                            new PaintDotNet.Imaging.ExifValue((PaintDotNet.Imaging.ExifValueType)this.Type,
                                                                                              (byte[])this.data.Clone()));
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataEntry entry && Equals(entry);
        }

        public bool Equals(MetadataEntry other)
        {
            if (other is null)
            {
                return false;
            }

            return this.Section == other.Section && this.TagId == other.TagId;
        }

        public byte[] GetData()
        {
            return (byte[])this.data.Clone();
        }

        public byte[] GetDataReadOnly()
        {
            return this.data;
        }

        public override int GetHashCode()
        {
            int hashCode = -2103575766;

            unchecked
            {
                hashCode = (hashCode * -1521134295) + this.Section.GetHashCode();
                hashCode = (hashCode * -1521134295) + this.TagId.GetHashCode();
            }

            return hashCode;
        }

        public static bool operator ==(MetadataEntry left, MetadataEntry right)
        {
            return EqualityComparer<MetadataEntry>.Default.Equals(left, right);
        }

        public static bool operator !=(MetadataEntry left, MetadataEntry right)
        {
            return !(left == right);
        }
    }
}
