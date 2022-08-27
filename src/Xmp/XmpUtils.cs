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

using PaintDotNet;
using PaintDotNet.Collections;
using PaintDotNet.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace MozJpegFileType.Xmp
{
    internal static partial class XmpUtils
    {
        private static readonly byte[] StandardXmpSignatureAscii;
        private static readonly int StandardXmpSignatureLengthWithTerminator;

        static XmpUtils()
        {
            StandardXmpSignatureAscii = Encoding.ASCII.GetBytes(XmpConstants.StandardXmpSignature);
            StandardXmpSignatureLengthWithTerminator = StandardXmpSignatureAscii.Length + 1;
        }

        public static byte[] AddSignatureToStandardXmpPacket(byte[] xmpPacketXmlUtf8)
        {
            byte[] xmpPacketWithSignature = new byte[StandardXmpSignatureLengthWithTerminator + xmpPacketXmlUtf8.Length];

            Array.Copy(StandardXmpSignatureAscii, xmpPacketWithSignature, StandardXmpSignatureAscii.Length);
            // Ensure the null terminator is present.
            xmpPacketWithSignature[StandardXmpSignatureLengthWithTerminator] = 0;

            Array.Copy(xmpPacketXmlUtf8, 0, xmpPacketWithSignature, StandardXmpSignatureLengthWithTerminator, xmpPacketXmlUtf8.Length);

            return xmpPacketWithSignature;
        }

        public static ExtendedXmpData SplitXmpPacketIntoExtendedXmp(byte[] xmpPacketXmlUtf8)
        {
            byte[] standardXmpPacketXmlUtf8;
            List<byte[]> extendedXmpChunks = new List<byte[]>();

            // Calculate MD5 hash string
            string md5HashString;
            {
                MD5 md5Hasher = MD5.Create();
                byte[] md5HashBytes = md5Hasher.ComputeHash(xmpPacketXmlUtf8);
                md5HashString = md5HashBytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)).Join(string.Empty);
                Debug.Assert(md5HashString.Length == 32);
            }

            standardXmpPacketXmlUtf8 = CreateStandardPacketForExtenededXmp(md5HashString);

            // Prepare some constants
            byte[] nsAdobeComXmpExtensionAscii = Encoding.ASCII.GetBytes(XmpConstants.ExtendedXmpChunkSignature);
            Debug.Assert(nsAdobeComXmpExtensionAscii.Length == 34);

            byte[] md5HashTextAscii = Encoding.ASCII.GetBytes(md5HashString);
            Debug.Assert(md5HashTextAscii.Length == 32);

            List<byte> chunkBuilder = new List<byte>(65535);

            // Serialize the real XMP packet data and then blast it out into APP1 chunks that are 64K each
            int chunkStartIndex = 0;
            while (chunkStartIndex < xmpPacketXmlUtf8.Length)
            {
                const int maxChunkSize = 65400;
                int chunkEndIndex = Math.Min(xmpPacketXmlUtf8.Length, chunkStartIndex + maxChunkSize);
                int chunkLength = chunkEndIndex - chunkStartIndex;
                chunkBuilder.Clear();

                // From section 1.1.3.1:
                // Each chunk is written into the JPEG file within a separate APP1 marker segment.
                // Each ExtendedXMP marker segment contains: ...

                // 1) A null-terminated signature string
                chunkBuilder.AddRange(nsAdobeComXmpExtensionAscii);
                chunkBuilder.Add(0);

                // 2) A 128-bit GUID stored as a 32-byte ASCII hex string, capital A-F, no null termination.
                //    The GUID is a 128-bit MD5 digest of the full ExtendedXMP serialization.
                chunkBuilder.AddRange(md5HashTextAscii); // not null terminated

                // 3) The full length of the ExtendedXMP serialization as a 32-bit unsigned integer
                AddBytes(chunkBuilder, UInt32Util.GetBytesBigEndian(checked((uint)xmpPacketXmlUtf8.Length)));

                // 4) The offset of this portion as a 32-bit unsigned integer.
                AddBytes(chunkBuilder, UInt32Util.GetBytesBigEndian(checked((uint)chunkStartIndex)));

                // 5) The ExtendedXMP chunk itself
                ArraySegment<byte> extXmpUtf8Segment = new ArraySegment<byte>(xmpPacketXmlUtf8, chunkStartIndex, chunkLength);
                chunkBuilder.AddRange(extXmpUtf8Segment);

                // Give data to caller
                extendedXmpChunks.Add(chunkBuilder.ToArrayEx());

                chunkStartIndex = chunkEndIndex;
            }

            return new ExtendedXmpData(standardXmpPacketXmlUtf8, extendedXmpChunks);
        }

        public static bool TryGetExtendedXmpGuid(XDocument document, out string extendedXmpGuid)
        {
            extendedXmpGuid = null;

            if (document is null)
            {
                return false;
            }

            XElement rdfElement = document.Document.Descendants(XmpConstants.RdfElementXName).First();

            extendedXmpGuid = TryGetExtendedXmpGuid(rdfElement);

            return !string.IsNullOrWhiteSpace(extendedXmpGuid);
        }

        public static ExtendedXMPChunk TryParseExtendedXmpData(byte[] extendedXmp, PaintDotNet.AppModel.IArrayPoolService arrayPool)
        {
            const int MD5LengthInBytes = 32;
            const int ExtendedXmpHeaderLength = MD5LengthInBytes + sizeof(uint) + sizeof(uint);

            if (extendedXmp.Length <= ExtendedXmpHeaderLength)
            {
                return null;
            }

            string md5 = Encoding.ASCII.GetString(extendedXmp, 0, MD5LengthInBytes);

            uint totalLength = ParseUInt32BigEndian(extendedXmp, MD5LengthInBytes);
            uint chunkOffset = ParseUInt32BigEndian(extendedXmp, MD5LengthInBytes + sizeof(uint));

            int dataLength = extendedXmp.Length - ExtendedXmpHeaderLength;

            IArrayPoolBuffer<byte> data = arrayPool.Rent<byte>(dataLength);

            Array.Copy(extendedXmp, ExtendedXmpHeaderLength, data.Array, 0, dataLength);

            return new ExtendedXMPChunk(md5, totalLength, chunkOffset, data);
        }

        public static XDocument TryParseXmpBytes(byte[] xmpBytes)
        {
            XDocument document = null;

            try
            {
                using (MemoryStream stream = new MemoryStream(xmpBytes))
                {
                    document = XDocument.Load(stream);
                }
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                // Ignore it.
            }

            return document;
        }

        public static XDocument MergeXmpPackets(XDocument standardXmpPacket, XDocument extendedXmpPacket)
        {
            XDocument mergedDocument = new XDocument(standardXmpPacket);
            XElement mergedXmpMetaElement = mergedDocument.Element(XmpConstants.XmpMetaElementXName);
            XElement mergedRdfElement = mergedXmpMetaElement.Element(XmpConstants.RdfElementXName);

            XDocument extendedDocument = extendedXmpPacket;
            XElement extendedXmpMetaElement = extendedDocument.Element(XmpConstants.XmpMetaElementXName);
            XElement extendedRdfElement = extendedXmpMetaElement.Element(XmpConstants.RdfElementXName);

            foreach (XElement element in extendedRdfElement.Elements(XmpConstants.DescriptionElementXName))
            {
                mergedRdfElement.Add(element);
            }

            XElement descriptionElement0 = mergedRdfElement.Elements(XmpConstants.DescriptionElementXName).FirstOrDefault();
            if (descriptionElement0 != null)
            {
                foreach (XElement descriptionElementN in mergedRdfElement.Elements(XmpConstants.DescriptionElementXName).Skip(1))
                {
                    if (CanMergeDescriptionElements(descriptionElement0, descriptionElementN))
                    {
                        BestFaithMergeElements(descriptionElement0, descriptionElementN);
                        descriptionElementN.Remove();
                    }
                }
            }

            foreach (XElement element in mergedRdfElement.Elements(XmpConstants.DescriptionElementXName))
            {
                TryRemoveHasExtendedXMPObject(element);
            }

            return mergedDocument;
        }

        private static void AddBytes(ICollection<byte> output, (byte b0, byte b1, byte b2, byte b3) bytes)
        {
            output.Add(bytes.b0);
            output.Add(bytes.b1);
            output.Add(bytes.b2);
            output.Add(bytes.b3);
        }

        private static void BestFaithMergeElements(XElement targetElement, XElement sourceElement)
        {
            foreach (XAttribute sourceAttribute in sourceElement.Attributes())
            {
                TryAddAttribute(targetElement, sourceAttribute);
            }

            foreach (XElement childSourceElement in sourceElement.Elements())
            {
                TryAddElement(targetElement, childSourceElement);
            }
        }

        private static bool CanMergeDescriptionElements(XElement element1, XElement element2)
        {
            XAttribute aboutUri1 = element1.Attribute(XmpConstants.AboutUriXName);
            XAttribute aboutUri2 = element2.Attribute(XmpConstants.AboutUriXName);

            if (aboutUri1 == null || aboutUri2 == null)
            {
                return true;
            }

            if (aboutUri1.Value.Equals(aboutUri2.Value, StringComparison.InvariantCulture))
            {
                return true;
            }

            return false;
        }

        private static byte[] CreateStandardPacketForExtenededXmp(string md5HashString)
        {
            string standardXmpPacketXmlBegin =
                    "<?xpacket begin=\"\xFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>" + Environment.NewLine +
                    "  <x:xmpmeta xmlns:x =\"adobe:ns:meta/\">" + Environment.NewLine +
                    "    <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">" + Environment.NewLine +
                   $"      <rdf:Description rdf:about=\"\" xmlns:xmpNote=\"http://ns.adobe.com/xmp/note/\" xmpNote:HasExtendedXMP=\"{md5HashString}\" />" + Environment.NewLine +
                    "    </rdf:RDF>" + Environment.NewLine +
                    "  </x:xmpmeta>";

            string standardXmpPacketXmlEnd = Environment.NewLine + "<?xpacket end=\"w\"?>";

            // Aim for having 1024 bytes total. Put padding spaces before the xpacket end. This allows modifying XMP without reencoding.
            int padding0 = 1024 - Encoding.UTF8.GetByteCount(standardXmpPacketXmlBegin) - Encoding.UTF8.GetByteCount(standardXmpPacketXmlEnd);
            int padding = Math.Max(0, padding0);

            string standardXmpPacketXml = standardXmpPacketXmlBegin + new string(' ', padding) + standardXmpPacketXmlEnd;

            return AddSignatureToStandardXmpPacket(Encoding.UTF8.GetBytes(standardXmpPacketXml));
        }

        private static uint ParseUInt32BigEndian(byte[] bytes, int startIndex)
        {
            return (uint)((bytes[startIndex] << 24) |
                          (bytes[startIndex + 1] << 16) |
                          (bytes[startIndex + 2] << 8) |
                           bytes[startIndex + 3]);
        }

        private static bool TryAddAttribute(XElement targetElement, XAttribute sourceAttribute)
        {
            XAttribute targetAttribute = targetElement.Attribute(sourceAttribute.Name);

            if (targetAttribute == null)
            {
                // Attribute does not exist in target -- add and we're done
                targetElement.Add(sourceAttribute);
                return true;
            }

            if (targetAttribute.Value.Equals(sourceAttribute.Value, StringComparison.InvariantCulture))
            {
                // Attribute exists in target and has the same value. No problem.
                return true;
            }

            return false;
        }

        private static bool TryAddElement(XElement targetElement, XElement sourceChildElement)
        {
            XElement targetChildElement = targetElement.Element(sourceChildElement.Name);

            if (targetChildElement == null)
            {
                // Element does not exist in target -- add and we're done
                targetElement.Add(sourceChildElement);
                return true;
            }

            if (targetChildElement.Value.Equals(sourceChildElement.Value, StringComparison.InvariantCultureIgnoreCase))
            {
                // Element exists in target and has the same value. No problem.
                return true;
            }

            Debug.Assert(false);
            return false;
        }

        private static string TryGetExtendedXmpGuid(XElement rdfElement)
        {
            // The xmpNote:HasExtendedXMP element may be located in any top-level rdf:Description element, not just the first one.

            foreach (XElement descriptionElement in rdfElement.Elements(XmpConstants.DescriptionElementXName))
            {
                XAttribute hasExtXmpAttr = descriptionElement.Attribute(XmpConstants.HasExtendedXmpObjectXName);
                if (hasExtXmpAttr != null)
                {
                    return hasExtXmpAttr.Value;
                }

                XElement hasExtXmpElement = descriptionElement.Element(XmpConstants.HasExtendedXmpObjectXName);
                if (hasExtXmpElement != null)
                {
                    return hasExtXmpElement.Value;
                }
            }

            return null;
        }

        private static bool TryRemoveHasExtendedXMPObject(XElement descriptionElement)
        {
            XAttribute hasExtXmpAttr = descriptionElement.Attribute(XmpConstants.HasExtendedXmpObjectXName);
            if (hasExtXmpAttr != null)
            {
                hasExtXmpAttr.Remove();
                return true;
            }

            XElement hasExtXmpElement = descriptionElement.Element(XmpConstants.HasExtendedXmpObjectXName);
            if (hasExtXmpElement != null)
            {
                hasExtXmpElement.Remove();
                return true;
            }

            return false;
        }
    }
}
