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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MozJpegFileType.Exif
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ExifValueCollectionDebugView))]
    internal sealed class ExifValueCollection
        : IEnumerable<MetadataEntry>
    {
        private readonly List<MetadataEntry> exifMetadata;

        public ExifValueCollection(List<MetadataEntry> items)
        {
            this.exifMetadata = items ?? throw new ArgumentNullException(nameof(items));
        }

        public int Count => this.exifMetadata.Count;

        public MetadataEntry GetAndRemoveValue(MetadataKey key)
        {
            MetadataEntry value = this.exifMetadata.Find(p => p.Section == key.Section && p.TagId == key.TagId);

            if (value != null)
            {
                this.exifMetadata.RemoveAll(p => p.Section == key.Section && p.TagId == key.TagId);
            }

            return value;
        }

        public void Remove(MetadataKey key)
        {
            this.exifMetadata.RemoveAll(p => p.Section == key.Section && p.TagId == key.TagId);
        }

        public IEnumerator<MetadataEntry> GetEnumerator()
        {
            return this.exifMetadata.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.exifMetadata.GetEnumerator();
        }

        private sealed class ExifValueCollectionDebugView
        {
            private readonly ExifValueCollection collection;

            public ExifValueCollectionDebugView(ExifValueCollection collection)
            {
                this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public MetadataEntry[] Items
            {
                get
                {
                    return this.collection.exifMetadata.ToArray();
                }
            }
        }
    }
}
