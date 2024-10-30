using Svg.Skia;
using SkiaSharp;
using System.Text;

class Program
{
    static readonly HttpClient httpClient = new();
    
    static async Task Main(string[] args)
    {
        var flagType = args.Length > 1 ? args[1] : "Eco";
        var totalImagesSaved = 0;
        var numberOfRuns = args.Length > 0 ? int.Parse(args[0]) : 1;

        for (int run = 0; run < numberOfRuns; run++)
        {
            var url = $"https://krcadinac.com/all-aligned/_generate_flags?vector=%5B%7B%22value%22%3A{(args.Length > 1 ? 1 : 0)}%2C%22key%22%3A%22{flagType}%22%2C%22type%22%3A%22unipolar%22%7D%5D";
            var response = await httpClient.GetStringAsync(url);
            var svgs = System.Text.Json.JsonSerializer.Deserialize<string[]>(response);

            if (svgs == null)
            {
                Console.WriteLine($"No flags found for {flagType} in run {run + 1}.");
                continue;
            }

            for (int i = 0; i < svgs.Length; i++)
            {
                var svg = svgs[i];
                SaveSvgAsCircularPng(svg, $"flag_{totalImagesSaved + i + 1}.png");
                Console.WriteLine($"Saved flag_{totalImagesSaved + i + 1}.png");
            }

            totalImagesSaved += svgs.Length;
        }
    }

    static void SaveSvgAsCircularPng(string svgContent, string filePath)
    {
        var svgBytes = Encoding.UTF8.GetBytes(svgContent);
        using var stream = new MemoryStream(svgBytes);

        var svg = new SKSvg();
        svg.Load(stream);

        var dimension = 128;
        using var bitmap = new SKBitmap(dimension, dimension, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(new SKImageInfo(dimension, dimension));

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using (var clipPath = new SKPath())
        {
            clipPath.AddCircle(dimension / 2f, dimension / 2f, dimension / 2f);
            canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);
        }

        var scaleX = dimension / svg.Picture.CullRect.Width;
        var scaleY = dimension / svg.Picture.CullRect.Height;
        var scale = Math.Max(scaleX, scaleY);

        var translateX = (dimension - svg.Picture.CullRect.Width * scale) / 2f;
        var translateY = (dimension - svg.Picture.CullRect.Height * scale) / 2f;

        canvas.Save();

        canvas.Translate(translateX, translateY);
        canvas.Scale(scale, scale);

        canvas.DrawPicture(svg.Picture);

        canvas.Restore();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        using var fileStream = File.OpenWrite(filePath);
        data.SaveTo(fileStream);
    }
}
