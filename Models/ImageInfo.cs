using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageViewer.Models;

public class ImageInfo {
	public int width { get; init; }
	public int height { get; init; }
	public string format { get; init; } = null!;
	public string filePath { get; init; } = null!;
	public string fileSize { get; init; } = null!;
	public long fileLength { get; init; }
	public int bitDepth { get; init; }
	public PixelFormat pixelFormat { get; init; }
	public BitmapImage? imageSource { get; init; }
	public string loadTime { get; init; } = null!;

	private ImageInfo() { }

	public static async Task<ImageInfo>
		FromAsync(string filePath, bool getImageSource = true, Rotation? rotation = null) {
		BitmapImage? img = null;
		Stopwatch sw = new();

		if (getImageSource) {
			sw.Start();
			img = await ImageInfo.GetImageAsync(filePath, rotation);
			sw.Stop();
		}

		string fileLoadTime = getImageSource ? $"{sw.Elapsed.TotalMilliseconds / 1000:0.0}秒" : "N/A";

		var decoder = BitmapDecoder.Create(
			new Uri(filePath),
			BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.DelayCreation,
			BitmapCacheOption.None);
		BitmapFrame? frame = decoder.Frames[0];

		var format = decoder.CodecInfo!.MimeTypes.Split("/")[1].ToLower();
		var fileLength = new FileInfo(filePath).Length;
		var fileSize = GetFormattedFileSize(fileLength);
		var width = frame.PixelWidth;
		var height = frame.PixelHeight;
		var bitDepth = frame.Format.BitsPerPixel;
		PixelFormat pixelFormat = frame.Format;

		return new ImageInfo {
			width = width,
			height = height,
			bitDepth = bitDepth,
			format = format,
			fileLength = fileLength,
			fileSize = fileSize,
			imageSource = img,
			filePath = filePath,
			pixelFormat = pixelFormat,
			loadTime = fileLoadTime
		};
	}

	public static string GetFormattedFileSize(long size) {
		string[] units = ["B", "KB", "MB", "GB", "TB"];
		int unitIndex = 0;

		double sizeInDouble = size;
		while (sizeInDouble >= 1024 && unitIndex < units.Length - 1) {
			sizeInDouble /= 1024;
			unitIndex++;
		}

		return $"{sizeInDouble:F2} {units[unitIndex]}";
	}

	public static Task<BitmapImage> GetImageAsync(string path, Rotation? rotation = null) {
		return Task.Run(() => {
			var bi = new BitmapImage();
			bi.BeginInit();
			if (rotation.HasValue) bi.Rotation = rotation.Value;
			bi.CacheOption = BitmapCacheOption.OnLoad;
			using Stream stream = new MemoryStream(File.ReadAllBytes(path));
			bi.StreamSource = stream;
			bi.EndInit();
			bi.Freeze();
			return bi;
		});
	}

	public static Task<(int width, int height)> GetImageDimensions(string path) {
		return Task.Run(() => {
			var decoder = BitmapDecoder.Create(
				new Uri(path),
				BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.DelayCreation,
				BitmapCacheOption.None);

			BitmapFrame frame = decoder.Frames[0];
			return (frame.PixelWidth, frame.PixelHeight);
		});
	}
}