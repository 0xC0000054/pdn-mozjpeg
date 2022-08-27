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
using PaintDotNet.AppModel;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.IO;

namespace MozJpegFileType
{

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    internal sealed class MozJpegFileTypePlugin : PropertyBasedFileType
    {
        private readonly IArrayPoolService arrayPoolService;

        /// <summary>
        /// Constructs a ExamplePropertyBasedFileType instance
        /// </summary>
        internal MozJpegFileTypePlugin(IFileTypeHost host)
            : base(
                "MozJpeg",
                new FileTypeOptions
                {
                    LoadExtensions = new string[] { ".jpg", ".jpeg", ".jpe", ".jfif" },
                    SaveExtensions = new string[] { ".jpg", ".jpeg", ".jpe", ".jfif" },
                    SupportsCancellation = true,
                    SupportsLayers = false
                })
        {
            this.arrayPoolService = host?.Services.GetService<IArrayPoolService>();
        }

        // Names of the properties
        private enum PropertyNames
        {
            Quality,
            ChromaSubsampling,
            Progressive
        }

        /// <summary>
        /// Add properties to the dialog
        /// </summary>
        public override PropertyCollection OnCreateSavePropertyCollection()
        {
            Property[] props = new Property[]
            {
                new Int32Property(PropertyNames.Quality, 75, 0, 100, false),
                CreateChromaSubsampling(),
                new BooleanProperty(PropertyNames.Progressive, false, false)
            };

            return new PropertyCollection(props);

            StaticListChoiceProperty CreateChromaSubsampling()
            {
                // The list is created manually because some of the YUVChromaSubsampling enumeration values
                // are used for internal signaling.

                object[] choiceValues = new object[]
                {
                    ChromaSubsampling.Subsampling420,
                    ChromaSubsampling.Subsampling422,
                    ChromaSubsampling.Subsampling444
                };

                int defaultChoiceIndex = Array.IndexOf(choiceValues, ChromaSubsampling.Subsampling422);

                return new StaticListChoiceProperty(PropertyNames.ChromaSubsampling, choiceValues, defaultChoiceIndex);
            }
        }

        /// <summary>
        /// Adapt properties in the dialog (DisplayName, ...)
        /// </summary>
        public override ControlInfo OnCreateSaveConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultSaveConfigUI(props);

            PropertyControlInfo qualityPCI = configUI.FindControlForPropertyName(PropertyNames.Quality);
            qualityPCI.ControlProperties[ControlInfoPropertyNames.DisplayName].Value = string.Empty;
            qualityPCI.ControlProperties[ControlInfoPropertyNames.Description].Value = "Quality";

            configUI.SetPropertyControlValue(PropertyNames.Quality, ControlInfoPropertyNames.DisplayName, "Quality");
            configUI.SetPropertyControlValue(PropertyNames.Quality, ControlInfoPropertyNames.Description, string.Empty);

            PropertyControlInfo chromaSubsamplingPCI = configUI.FindControlForPropertyName(PropertyNames.ChromaSubsampling);
            chromaSubsamplingPCI.ControlProperties[ControlInfoPropertyNames.DisplayName].Value = "Chroma Subsampling";
            chromaSubsamplingPCI.ControlProperties[ControlInfoPropertyNames.Description].Value = string.Empty;
            chromaSubsamplingPCI.SetValueDisplayName(ChromaSubsampling.Subsampling420, "4:2:0 (Best Compression)");
            chromaSubsamplingPCI.SetValueDisplayName(ChromaSubsampling.Subsampling422, "4:2:2");
            chromaSubsamplingPCI.SetValueDisplayName(ChromaSubsampling.Subsampling444, "4:4:4 (Best Quality)");

            PropertyControlInfo progressivePCI = configUI.FindControlForPropertyName(PropertyNames.Progressive);
            progressivePCI.ControlProperties[ControlInfoPropertyNames.DisplayName].Value = string.Empty;
            progressivePCI.ControlProperties[ControlInfoPropertyNames.Description].Value = "Progressive";

            return configUI;
        }

        /// <summary>
        /// Creates a document from a stream
        /// </summary>
        protected override Document OnLoad(Stream input)
        {
            return MozJpegFile.Load(input, this.arrayPoolService);
        }

        /// <summary>
        /// Saves a document to a stream respecting the properties
        /// </summary>
        protected override void OnSaveT(Document input,
                                        Stream output,
                                        PropertyBasedSaveConfigToken token,
                                        Surface scratchSurface,
                                        ProgressEventHandler progressCallback)
        {
            int quality = token.GetProperty<Int32Property>(PropertyNames.Quality).Value;
            ChromaSubsampling chromaSubsampling = (ChromaSubsampling)token.GetProperty(PropertyNames.ChromaSubsampling).Value;
            bool progressive = token.GetProperty<BooleanProperty>(PropertyNames.Progressive).Value;

            MozJpegFile.Save(input,
                             output,
                             scratchSurface,
                             quality,
                             chromaSubsampling,
                             progressive,
                             progressCallback,
                             this.arrayPoolService);
        }
    }
}
