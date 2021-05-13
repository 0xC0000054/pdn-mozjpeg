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

using PaintDotNet;
using System;
using System.Reflection;

namespace MozJpegFileType
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        private readonly Assembly assembly = typeof(PluginSupportInfo).Assembly;

        public string Author => this.assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;

        public string Copyright => this.assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;

        public string DisplayName => this.assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;

        public Version Version => this.assembly.GetName().Version;

        public Uri WebsiteUri => new Uri("https://www.getpaint.net/redirect/plugins.html");
    }
}
