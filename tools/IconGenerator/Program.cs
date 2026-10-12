using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: IconGenerator <source.png> <output.ico>");
    return 2;
}

var sourcePath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Source artwork not found: {sourcePath}");
    return 3;
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

int[] sizes = [256, 128, 64, 48, 32, 24, 20, 16];
using var source = new Bitmap(sourcePath);
var images = sizes.Select(size => Render(source, size)).ToArray();

using (var output = File.Create(outputPath))
using (var writer = new BinaryWriter(output))
{
    writer.Write((ushort)0);
    writer.Write((ushort)1);
    writer.Write((ushort)sizes.Length);

    var offset = 6 + (16 * sizes.Length);
    for (var index = 0; index < sizes.Length; index++)
    {
        var encodedSize = sizes[index] == 256 ? (byte)0 : (byte)sizes[index];
        writer.Write(encodedSize);
        writer.Write(encodedSize);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write((uint)images[index].Length);
        writer.Write((uint)offset);
        offset += images[index].Length;
    }

    foreach (var image in images)
    {
        writer.Write(image);
    }
}

Console.WriteLine(outputPath);
return 0;

static byte[] Render(Image source, int size)
{
    using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var graphics = Graphics.FromImage(bitmap);
    graphics.Clear(Color.Transparent);
    graphics.CompositingMode = CompositingMode.SourceCopy;
    graphics.CompositingQuality = CompositingQuality.HighQuality;
    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    graphics.SmoothingMode = SmoothingMode.HighQuality;
    graphics.DrawImage(source, new Rectangle(0, 0, size, size));

    using var buffer = new MemoryStream();
    bitmap.Save(buffer, ImageFormat.Png);
    return buffer.ToArray();
}
