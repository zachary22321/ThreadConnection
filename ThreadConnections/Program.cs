using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class Program
{
    static void Main()
    {
        string inputFile = @"/Users/zacharypozzi/Downloads/cb/cb.bmp";
        if (!File.Exists(inputFile))
        {
            Console.WriteLine("Input file does not exist.");
            return;
        }

        Image<Rgba32> image;
        try
        {
            image = Image.Load<Rgba32>(inputFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading image: {ex.Message}");
            return;
        }

        int width = image.Width;
        int height = image.Height;

        HashSet<Rgba32> colors = new HashSet<Rgba32>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colors.Add(image[x, y]);
            }
        }

        string baseName = Path.GetFileNameWithoutExtension(inputFile);
        string dir = Path.GetDirectoryName(inputFile);

        List<string> outFiles = new List<string>();

        foreach (Rgba32 c in colors)
        {
            Image<Rgba32> newImage = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));

            // Set original positions of the color
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (image[x, y].Equals(c))
                    {
                        newImage[x, y] = c;
                    }
                }
            }

            // Now perform the filling per row
            for (int y = 0; y < height; y++)
            {
                List<int> pos = new List<int>();
                for (int x = 0; x < width; x++)
                {
                    if (image[x, y].Equals(c))
                    {
                        pos.Add(x);
                    }
                }

                if (pos.Count < 2) continue;

                // Fill non-wrap gaps
                for (int i = 0; i < pos.Count - 1; i++)
                {
                    int spaces = pos[i + 1] - pos[i] - 1;
                    if (spaces < 20 && spaces > 0)
                    {
                        for (int fillx = pos[i] + 1; fillx < pos[i + 1]; fillx++)
                        {
                            newImage[fillx, y] = c;
                        }
                    }
                }

                // Fill wrap-around if applicable
                int spaces_wrap = pos[0] + (width - pos[pos.Count - 1] - 1);
                if (spaces_wrap < 20 && spaces_wrap > 0)
                {
                    // Fill from end to right edge
                    for (int fillx = pos[pos.Count - 1] + 1; fillx < width; fillx++)
                    {
                        newImage[fillx, y] = c;
                    }
                    // Fill from left edge to start
                    for (int fillx = 0; fillx < pos[0]; fillx++)
                    {
                        newImage[fillx, y] = c;
                    }
                }
            }

            // Save the new PNG
            string outFile = Path.Combine(dir, $"{baseName}_{c.R}_{c.G}_{c.B}.png");
            newImage.SaveAsPng(outFile);
            outFiles.Add(outFile);
            newImage.Dispose();
        }

        image.Dispose();

        // Now find and delete the PNG with the least transparent pixels
        if (outFiles.Count > 0)
        {
            string toDelete = null;
            int minTransparent = int.MaxValue;

            foreach (var file in outFiles)
            {
                using (var img = Image.Load<Rgba32>(file))
                {
                    int trans = 0;
                    for (int y = 0; y < img.Height; y++)
                    {
                        for (int x = 0; x < img.Width; x++)
                        {
                            if (img[x, y].A == 0)
                            {
                                trans++;
                            }
                        }
                    }
                    if (trans < minTransparent)
                    {
                        minTransparent = trans;
                        toDelete = file;
                    }
                }
            }

            if (toDelete != null)
            {
                File.Delete(toDelete);
            }
        }

        Console.WriteLine("Processing complete.");
    }
}