# pdn-mozjpeg

A [Paint.NET](http://www.getpaint.net) filetype plugin that saves JPEG images using the [mozjpeg](https://github.com/mozilla/mozjpeg) encoder.

## Installation

1. Close Paint.NET.
2. Place MozJpegFileType.dll, MozJpegFileTypeIO_x86.dll and MozJpegFileTypeIO_x64.dll in the Paint.NET FileTypes folder which is usually located in one the following locations depending on the Paint.NET version you have installed.

  Paint.NET Version |  FileTypes Folder Location
  --------|----------
  Classic | C:\Program Files\Paint.NET\FileTypes    
  Microsoft Store | Documents\paint.net App Files\FileTypes

3. Restart Paint.NET.
4. The filetype should now be available as the "MozJpeg" item in the save dialog.

## License

This project is licensed under the terms of the MIT License.   
See [License.txt](License.txt) for more information.

# Source code

## Prerequisites

* Visual Studio 2019
* Paint.NET 4.2.16 or later

## Building the plugin

* Open the solution
* Change the PaintDotNet references in the MozJpegFileType project to match your Paint.NET install location
* Update the post build events to copy the build output to the Paint.NET FileTypes folder
* Build the solution

## 3rd Party Code

This project uses the following libraries. (the required header and library files are located in the `3rd-party` sub-folders).

* [mozjpeg](https://github.com/mozilla/mozjpeg) (v4.0.3 tag)