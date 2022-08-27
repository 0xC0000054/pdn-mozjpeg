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

using System;
using System.Diagnostics;

namespace MozJpegFileType.Exif
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    internal readonly struct MetadataKey
        : IEquatable<MetadataKey>
    {
        public MetadataKey(MetadataSection section, ushort tagId)
        {
            this.Section = section;
            this.TagId = tagId;
        }

        public MetadataSection Section { get; }

        public ushort TagId { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return string.Format("{0}, Tag# {1} (0x{1:X})", this.Section, this.TagId);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataKey other && Equals(other);
        }

        public bool Equals(MetadataKey other)
        {
            return this.Section == other.Section && this.TagId == other.TagId;
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

        public static bool operator ==(MetadataKey left, MetadataKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MetadataKey left, MetadataKey right)
        {
            return !(left == right);
        }
    }
}
