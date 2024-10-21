# Steganography Library
---Note - this is a very early build, still very much in development---
## What is included in the library
* SLFileInfo class
* encode method
* decode method

## SLFileInfo class:
* Used to hold data for a file; is the output of the decode method.
* Has two read-only attributes: fileName, data
* fileName
  * Type: string
  * Stores the file name of the file referred to by the SLFileInfo class
* data
  * Type: byte[]
  * Stores the data for the file in a byte array

## encode method
* Takes two parameters: img and filePath
* img
  * type: SKBitmap
  * is a bitmap of the image you want to encode the file to
* filepath
  * type: string
  * is the path for the file you want to encode into the image
* returns SKBitmap
  * this is the image with the data encoded.

## decode method
* Takes one parameter: img
* img
  * type: SKBitmap
  * is the image you want to decode the file from.
* returns SLFileInfo
  * this has the filename of the decoded file, and the byte array of the file.
___
The SteganographyTest folder is a quick program which allows for use of the library. Use it from the command line with these arguments:
To encode:
SteganographyTest.exe enc \<File Path\> \<Image Path\> \<Output Directory\>

To decode:
SteganographyTest.exe dec \<Image path\> \<Output Directory\>
