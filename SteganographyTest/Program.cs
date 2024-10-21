namespace SteganographyTest
{
    using SkiaSharp;
    using SteganographyLibrary;
    internal class Program
    {
        static void Main(string[] args)
        {
            static void encode(string[] args)
            {
                string filePath = args[1];
                string imgPath = args[2];
                string outputDirectory = args[3];
                SKBitmap map;
                using (FileStream img = File.OpenRead(imgPath))
                {
                    map = SKBitmap.Decode(img);
                }
                map = SteganographyLib.encode(map, filePath);
                map.Encode(SKFileWStream.OpenStream(Path.Join(outputDirectory, "encoded.png")), SKEncodedImageFormat.Png, 100);
            }
            static void decode(string[] args)
            {
                string imgPath = args[1];
                string outputDirectory = args[2];
                SKBitmap map;
                using (FileStream img = File.OpenRead(imgPath))
                {
                    map = SKBitmap.Decode(img);
                }
                SteganographyLib.SLFileInfo output = SteganographyLib.Decode(map);
                File.WriteAllBytes(Path.Join(outputDirectory, output.fileName), output.data);
            }
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments specified.");
                return;
            }
            if (args[0] == "enc")
            {
                if (args.Length != 4)
                {
                    Console.WriteLine("Expected 4 arguments. Received " + args.Length.ToString());
                    return;
                }
                encode(args);
            }
            if (args[0] == "dec")
            {
                if (args.Length != 3)
                {
                    Console.WriteLine("Expected 3 arguments. Received " + args.Length.ToString());
                    return;
                }
                decode(args);
            }
        }
    }
}
