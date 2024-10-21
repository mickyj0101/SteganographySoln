namespace SteganographyLibrary
{
    using SkiaSharp;
    using System.Diagnostics;
    using System.Text;

    public static class SteganographyLib
    {
        //SLFileInfo class stores a filename and a byte array for the data of a file.
        public class SLFileInfo
        {
            public string fileName { get; }
            public byte[] data { get; }
            public SLFileInfo(string fileName, byte[] data)
            {
                this.fileName = fileName;
                this.data = data;
            }
        }           
        //Class to store a 6-bit number with useful methods.
        private class SixBit
        {
            //int to store the 6-bit number
            private int val;
            
            //Constructor
            public SixBit(int val = 0)
            {
                if (val < 0 || val > 63)
                {
                    throw new ArgumentOutOfRangeException("'val' parameter must be between 0 and 63. Was " + val.ToString());
                }
                this.val = val;
            }
            public SixBit(uint val)
            {
                if (val > 63)
                {
                    throw new ArgumentOutOfRangeException("'val' parameter must be between 0 and 63. Was " + val.ToString());
                }
                this.val = (int)val;
            }
            //Set method
            public void setVal(int newVal)
            {
                if (newVal < 0 || newVal > 63)
                {
                    throw new ArgumentOutOfRangeException("'newVal' parameter must be between 0 and 63. Was " + val.ToString());
                }
                val = newVal;
            }
            //Get method
            public int getVal()
            {
                return val;
            }
            //Add a number to the val
            public void addToVal(int num)
            {
                setVal(num + val);
            }
            //Get the first two bits of the 6-bit number. Will be stored in the R value of a pixel.
            public int getRbits()
            {
                return val >> 4;
            }
            //Get the second two bits of the 6-bit number. Will be stored in the G value of a pixel.
            public int getGbits()
            {
                return (val & 12) >> 2;
            }
            //Get the third two bits of the 6-bit number. Will be stored in the B value of a pixel.
            public int getBbits()
            {
                return val & 3;
            }
            //Write data to a pixel
            public void writeToPixel(SKBitmap map, int x, int y)
            {
                if (x == 0 && y == 0) 
                {
                    Console.WriteLine("Writing to 0, 0");
                    Console.WriteLine(Convert.ToString(val, 2));
                    //Get the pixel being written to
                    SKColor pix = map.GetPixel(x, y);
                    //Get the r, g and b values to modify them
                    byte r = pix.Red;
                    byte g = pix.Green;
                    byte b = pix.Blue;
                    Console.WriteLine(Convert.ToString(r, 2));
                    Console.WriteLine(Convert.ToString(g, 2));
                    Console.WriteLine(Convert.ToString(b, 2));
                    //Use bitwise AND with ~3 to set the final two bits to 0, then use bitwise OR to set the final two bits to the bits for that colour, for each colour.
                    r = (byte)((r & ~3) | getRbits());
                    Console.WriteLine(Convert.ToString(r, 2));
                    g = (byte)((g & ~3) | getGbits());
                    Console.WriteLine(Convert.ToString(g, 2));
                    b = (byte)((b & ~3) | getBbits());
                    Console.WriteLine(Convert.ToString(b, 2));
                    //Set the pixel to a pixel with the new r, g and b values.
                    map.SetPixel(x, y, new SKColor(r, g, b, pix.Alpha));
                }
                else
                {
                    //Get the pixel being written to
                    SKColor pix = map.GetPixel(x, y);
                    //Get the r, g and b values to modify them
                    byte r = pix.Red;
                    byte g = pix.Green;
                    byte b = pix.Blue;
                    //Use bitwise AND with ~3 to set the final two bits to 0, then use bitwise OR to set the final two bits to the bits for that colour, for each colour.
                    r = (byte)((r & ~3) | getRbits());
                    g = (byte)((g & ~3) | getGbits());
                    b = (byte)((b & ~3) | getBbits());
                    //Set the pixel to a pixel with the new r, g and b values.
                    map.SetPixel(x, y, new SKColor(r, g, b, pix.Alpha));
                }
            }
            public void writeToPixel(SKBitmap map, int[] coords)
            {
                if (coords.Length != 2)
                {
                    throw new ArgumentException("'coords' argument must have length = 2. Had length = " + coords.Length.ToString());
                }
                writeToPixel(map, coords[0], coords[1]);
            }
        }     
        //Extension method to write an array of SixBits to an image, starting at certain co-ordinates.
        //Returns the co-ordinates of the pixel after the last pixel with data writen. For example: if you write 10 SixBits of data with startCoords[0, 0]
        //It will return [10, 0] as this is the first pixel without data written.
        static int[] writeToImage(this SixBit[] data, SKBitmap map, int[] startCoords)
        {
            int[] currentCoords = new int[startCoords.Length];
            Array.Copy(startCoords, currentCoords, startCoords.Length);
            foreach (SixBit pix in data)
            {
                pix.writeToPixel(map, currentCoords);
                try
                {
                    currentCoords.nextPixel(map.Width, map.Height);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    throw new ArgumentOutOfRangeException("Data is too large to fit in the image");
                }
            }
            return currentCoords;
        }
        //Gets a byte array as SixBits.
        static SixBit[] toSixBits(this byte[] data)
        {
            bool littleEndian = BitConverter.IsLittleEndian;
            //Get the number of bytes in the data
            int len = data.Length;
            //Create an array of SixBits, with length equal to the ceiling of len * 8 / 6f (which is the number of SixBits that will be in the resulting array.)
            SixBit[] result = new SixBit[(int)Math.Ceiling(len * 8 / 6f)];
            //Initialise i to track position in the data array, and x to track position in the result array. Don't use a for loop as i and x are needed after.
            int i = 0;
            int x = 0;
            //Repeat until i is too large to do the first line of the loop
            while (i < len - 2) 
            {
                byte[] someBytes = { 0, data[i], data[i + 1], data[i + 2] };
                if (littleEndian)
                {
                    Array.Reverse(someBytes);
                }
                //Convert the set of three bytes to an integer for easier access.
                uint threeBytes = (uint)BitConverter.ToInt32(someBytes, 0);               
                //Set the next 4 values of the result array to the next 4 sets of 6 bits.
                result[x] = new SixBit(threeBytes >> 18);
                result[x + 1] = new SixBit((threeBytes >> 12) & 63);
                result[x + 2] = new SixBit((threeBytes >> 6) & 63);
                result[x + 3] = new SixBit(threeBytes & 63);
                //Update i and x for the next iteration
                i += 3;
                x += 4;
            }
            //There may be some bytes remaining in the array if it was not a multiple of 3 in length.
            int remainingBytes = len - i;
            if (remainingBytes == 0) return result;
            //If there are remaining bytes, deal with it by manually creating SixBits and padding the last one.
            result[x] = new SixBit((data[i] >> 2) & 63);
            if (remainingBytes == 1)
            {
                result[x + 1] = new SixBit((data[i] & 3) << 4);
                return result;
            }
            else
            {
                result[x + 1] = new SixBit(((data[i] & 3) << 4) + data[i + 1] >> 4);
                result[x + 2] = new SixBit((data[i + 1] & 15) << 2);
                return result;
            }

        }
        //Extension method to get an int as an array of sixbits. If the 'numSixBits' parameter is 0 or blank, then it will return the minimum number of sixbits.
        //Otherwise, it will return that many sixbits. NOTE - it will not throw an error if the number specified is fewer than would be required to fit the data.
        //It will get the final 'numSixBits' SixBits of the integer.
        static SixBit[] toSixBits(this uint value, int numSixBits = 0)
        {
            if (numSixBits < 0) throw new ArgumentOutOfRangeException("'numSixBits' argument must be 0 or greater");
            if (numSixBits == 0)
            {
                uint temp = value;
                while (temp > 0)
                {
                    numSixBits++;
                    temp >>= 6;
                }
            }
            SixBit[] result = new SixBit[numSixBits];
            for (int i = 0; i < numSixBits; i += 1)
            {
                int pushLeft = 6 * i;
                if (pushLeft > 30)
                {
                    result[numSixBits - (i + 1)] = new SixBit();
                    continue;
                }
                if (pushLeft == 30)
                {
                    result[numSixBits - (i + 1)] = new SixBit((value & ((uint)3 << 30)) >> 30);
                    continue;
                }
                result[numSixBits - (i + 1)] = new SixBit((value & ((uint)63 << pushLeft)) >> pushLeft);
            }
            return result;

        }
        //Convert the pixel number (indexed from 0) to a set of co-ords
        static int[] pixelNumtoCoords(int width, int height, int num)
        {
            if (num >= width * height)
            {
                throw new ArgumentOutOfRangeException("'num' parameter must be less than width * height");
            }
            int rows = num / width;
            int cols = num % width;
            return [cols, rows];
        }
        //Extension method - go from a set of coords to the next pixel without converting pixel num to coords (as that other method has division and modulus which are slow)
        static int[] nextPixel(this int[] currentPixel, int width, int height)
        {
            if (currentPixel.Length != 2)
            {
                throw new ArgumentException("'currentPixel' argument must have length = 2");
            }
            int x = currentPixel[0]++;
            int y = currentPixel[1];
            if (x >= width)
            {
                y = currentPixel[1]++;
                currentPixel[0] = 0;
                x = 0;
            }
            if (y >= height)
            {
                throw new ArgumentOutOfRangeException("This would go beyond the width and height specified.");
            }
            return [x, y];
        }
        //Reads a single pixel into a SixBit object - returns the SixBit object and the co-ords of the next pixel.
        static (SixBit, int[]) readPixel(SKBitmap bitmap, int[] coords)
        {
            int[] copy = new int[2];
            Array.Copy(coords, copy, 2);
            SixBit result = new SixBit();
            SKColor pix = bitmap.GetPixel(coords[0], coords[1]);
            result.addToVal((pix.Red & 3) << 4);
            result.addToVal((pix.Green & 3) << 2);
            result.addToVal(pix.Blue & 3);
            copy.nextPixel(bitmap.Width, bitmap.Height);
            return (result, copy);
        }
        //Reads a span of pixels into an array of SixBit objects - Returls the array of SixBits and the co-ords of the next pixel after the span.
        static (SixBit[], int[]) readSpan(SKBitmap bitmap, int[] from, int numPixels)
        {
            int[] coords = new int[2];
            Array.Copy(from, coords, 2);
            SixBit[] result = new SixBit[numPixels];
            for (int i = 0; i < numPixels; i++)
            {
                result[i] = readPixel(bitmap, coords).Item1;
                coords.nextPixel(bitmap.Width, bitmap.Height);
            }
            return (result, coords);
        }
        //Returns an unsigned integer from an array of 6 or fewer SixBits. Assumes no padding to the right of the data.
        static uint toUint(this SixBit[] sixBits)
        {
            if (sixBits.Length > 6)
            {
                throw new ArgumentException("Length of SixBit array must be <= 6");
            }
            uint result = 0;
            for (int i = 0; i < sixBits.Length; i++)
            {
                result += (uint)sixBits[i].getVal() << (6 * (sixBits.Length - i - 1));
            }
            return result;
        }
        static uint toUint(this SixBit[] sixBits, int start, int length)
        {
            if (length > 6)
            {
                throw new ArgumentException("specified range must be <= 6 in length");
            }
            uint result = 0;
            for (int i = start; i < start + length; i++)
            {
                result += (uint)sixBits[i].getVal() << (6 * (start + length - 1 - i));
            }
            return result;
        }
        //Returns an array of bytes from an array of SixBits. Assumes padding to the right of the data.
        static byte[] toByteArray(this SixBit[] sixBits)
        {
            //Get the number of bits appended
            int appendedBits = 0;
            float testVal = sixBits.Length * 0.75f;
            if (testVal != Math.Floor(testVal))
            {
                appendedBits += 2;
                testVal -= 0.25f;
                if (testVal != Math.Floor(testVal))
                {
                    appendedBits += 2;
                }
            }
            //i is the position in the SixBit array. x is the position in the byte array.
            int i = 0;
            int x = 0;
            //Create result array.
            byte[] result = new byte[((sixBits.Length * 6) - appendedBits) / 8];
            //Read in groups of 4 SixBits (3 bytes) until there are fewer than 4 remaining.
            while (i < sixBits.Length - 3)
            {
                //Read 4 sixbits into a uint value
                uint threeBytes = sixBits.toUint(i, 4);
                //Set result[x], result[x + 1] and result[x + 2] to the bytes in the uint.
                result[x] = (byte)(threeBytes >> 16);
                result[x + 1] = (byte)((threeBytes >> 8) & 0xFF);
                result[x + 2] = (byte)(threeBytes & 0xFF);
                //Increment i and x
                x += 3;
                i += 4;
            }
            //Find remaining number of SixBits.
            int remainingSixBits = sixBits.Length - i;
            //Get the last remaining bytes from the SixBit array.
            uint lastBytes = sixBits.toUint(i, remainingSixBits);
            //Remove the appended bits.
            lastBytes >>= appendedBits;
            //Get the number of bytes in the lastBytes variable
            int numBytes = ((remainingSixBits * 6) - appendedBits) / 8;
            //Read these bytes into the last part of the result array
            for (int j = numBytes - 1; j >= 0; j--)
            {
                result[x + j] = (byte)((lastBytes >> 8 * (numBytes - j - 1)) & 0xFF);
            }
            return result;
        }
        //Encode the file at 'filepath' into the image 'img'
        public static SKBitmap encode(SKBitmap img, string filePath)
        {
            //System for encoding:
            //Each pixel has the final 2 bits of its R, G and B channels modified to store data.
            //This means that each pixel can store a number between 0 and 63 (including)
            //Writing:
            //First two pixels are used to store the number of pixels used to hold the filename. This means that up to 4095 pixels can be used for the filename.
            //The number of characters this can hold depends on the number of bits per character, as NTFS and FAT32 use unicode.
            //But 4095 pixels is 767 characters if each character uses the maximum 32 bits. This should be enough.
            //
            //The filename will be converted into bytes and then converted into chunks of 6 bits.
            //This will be done by dividing the byte array into sets of 3 bytes, which makes 4 6-bit chunks.
            //If there is 1 byte left, its first 6 bits make a chunk, then the remaining 2 have 4 zeros appended and make another chunk.
            //If there are 2 bytes left, their first 12 bits make a chunk, then the remaining 4 have 2 zeros appended and make another chunk.
            //If there are 0 bytes left, nothing needs to happen.
            //Starting from the 3rd pixel, write these 6-bit chunks one pixel at a time.
            //After this, we need to write the actual binary data within the file. A similar method will be employed.
            //Get the binary data as a byte array, and convert to 6-bit chunks in the same way.
            //Any appended bits are, once again, logged.
            //Then use 6 pixels to store the number of pixels holding data
            //This allows for 36 bits (though only 32 will be used), a great deal of redundancy as the 32-bit integer limit is over 517 times the number of pixels
            //in a 4k display.
            //After this, write the binary data.
            //
            //Reading:
            //Read the first two pixels to see the length of the filename in pixels.
            //Read through that many pixels to find the filename (unicode)
            //How to check how many bits were appended:
            //The result is either 0, 2, or 4 bits longer than a multiple of 8 bytes.
            //Multiply the number of chunks by 6, then check divisibility by 8.
            //If it is divisible, then no bits were appended. If not, subtract 2 and check again.
            //If it is divisible, then 2 bits were appended. If not, 4 bits were appended.
            //In practice, this can be done by multiplying the number of chunks by 0.75 (6/8) to check if it is divisible by 8.
            //If so, then no bits were appended. If not, subtract 2/8 (0.25) and check again.
            //If it is divisible, then 2 bits were appended. If not, 4 bits were appended.
            //Use this to determine the appended bits for the filename.
            //Then convert the chunks of 6 bits into bytes to get the filename
            //Read the next 10 pixels to get the number of pixels holding data
            //Read the actual data
            //Use the same method to determine appended bits
            //Convert to bytes
            //Write the data to a file with the filename from the image.
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("filePath argument does not refer to an existing file.");
            }
            SKBitmap copy = img.Copy();
            //Get the filename as a string and byte array, then get the data from the file as a byte array. Though you should typically never read a whole file into
            //memory, in this case it should be fine as the image itself takes up much more RAM than the file being written to it.
            //Essentially, this form of steganography only really works with relatively small files.
            string fileName = Path.GetFileName(filePath);
            byte[] nameData = Encoding.UTF8.GetBytes(fileName);
            byte[] fileData = File.ReadAllBytes(filePath);
            //Get the filename data as an array of SixBits.
            SixBit[] fileNameAsSixbits = nameData.toSixBits();
            uint nameDataLength = (uint)fileNameAsSixbits.Length;
            //Encode the length of the filename into two sixbits.
            SixBit[] firstTwo = nameDataLength.toSixBits(2);
            //Store the width and height of the image as separate variables, just so that it is easier to use.
            int width = copy.Width;
            int height = copy.Height;
            //Set the starting co-ordinates to [0, 0] or the top-left pixel.
            int[] coords = [0, 0];
            //Write the first two SixBits to the image.
            coords = firstTwo.writeToImage(copy, coords);
            Console.WriteLine(readPixel(copy, [0, 0]).Item1.getVal());
            //Write the filename to the image.
            coords = fileNameAsSixbits.writeToImage(copy, coords);
            //Get the file data as sixbits
            SixBit[] fileDataAsSixbits = fileData.toSixBits();
            //Work out how many pixels are needed to store the file data
            uint numPixels = (uint)fileDataAsSixbits.Length;
            //Get the number of pixels to store the file data as a SixBit array
            SixBit[] numPixelsSixbits = numPixels.toSixBits(6);
            //Write the number of pixels, then the file data, to the image.
            coords = numPixelsSixbits.writeToImage(copy, coords);
            coords = fileDataAsSixbits.writeToImage(copy, coords);
            return copy;
        }        
        //Decode an image into a SLFileInfo (which contains a string for the filename and a byte array for the data.)
        public static SLFileInfo Decode(SKBitmap img)
        {
            Console.WriteLine(readPixel(img, [0, 0]).Item1.getVal());
            //Set the current co-ords to (0, 0)
            int[] currentPos = [0, 0];
            //Read the first two pixels and update co-ords
            (SixBit[], int[]) FirstTwo = readSpan(img, currentPos, 2);
            currentPos = FirstTwo.Item2;
            //Get the number of pixels to store the filename
            uint numPixelsForFilename = FirstTwo.Item1.toUint();
            //Read the pixels storing the filename and update co-ords.
            (SixBit[], int[]) fileNameData = readSpan(img, currentPos, (int)numPixelsForFilename);
            currentPos = fileNameData.Item2;
            //Get the data for the filename in a byte array
            byte[] fileNameBytes = fileNameData.Item1.toByteArray();
            //Read the next 6 pixels and update co-ords
            (SixBit[], int[]) dataData = readSpan(img, currentPos, 6);
            currentPos = dataData.Item2;
            //Get the number of pixels used to store actual data.
            uint numPixelsForData = dataData.Item1.toUint();
            //read the pixels storing actual data
            (SixBit[], int[]) data = readSpan(img, currentPos, (int)numPixelsForData);
            //Get the data as a byte array
            byte[] dataBytes = data.Item1.toByteArray();
            //Get the filename as a string
            string fileName = Encoding.UTF8.GetString(fileNameBytes);
            //Return the filename and the data.
            return new SLFileInfo(fileName, dataBytes);
        }
    }
}
