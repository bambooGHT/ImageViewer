using System.Windows;
using System.Windows.Media.Imaging;
using ImageViewer.Models;

namespace ImageViewer.viewModels;

public partial class MainViewModel {
	private const double ImageMaxCacheCount = 12.0;

	private List<ImageItem> _imageList = [];
	private double _imgListPercentageRatio = 100.0;

	public List<ImageItem> imageList {
		get => _imageList;
		set {
			_imageList = value;
			OnPropertyChanged();
		}
	}
	public double imgListPercentageRatio {
		get => _imgListPercentageRatio;
		set {
			if (value.Equals(_imgListPercentageRatio)) return;
			value = value switch {
				> 200 => 200,
				< 10 => 10,
				_ => value
			};
			_imgListPercentageRatio = value;
			OnPropertyChanged();
		}
	}

	private int oldLeft;
	private int oldRight;

	private async Task InitScrollView(string[] paths, (int index, ImageInfo image)[]? loadedItem = null) {
		oldLeft = 0;
		oldRight = 0;
		imgListPercentageRatio = 100.0;

		var (width, height) = await ImageInfo.GetImageDimensions(paths[paths.Length / 2]);
		this.UpdateWindowSize(Math.Clamp(width + 50, 650, SystemParameters.WorkArea.Size.Width * 0.4),
			SystemParameters.WorkArea.Size.Height - this.spaceOccupiedHeight);

		var newWidth = (this.windowWidth - this.spaceOccupiedWidth) * (imgListPercentageRatio / 100);
		var newHeight = height * (newWidth / width);
		var list = paths.Select((p, index) => new ImageItem {
			index = index + 1,
			imagePath = p,
			width = newWidth,
			height = newHeight,
			originalWidth = width,
			originalHeight = height
		}).ToList();

		if (loadedItem != null) {
			foreach ((var index, ImageInfo item) in loadedItem) {
				ImageItem imageItem = list[index];
				imageItem.height = item.height * (newWidth / item.width);
				imageItem.originalWidth = item.width;
				imageItem.originalHeight = item.height;
				imageItem.imageSource = item.imageSource;
			}
		}

		_imageList.Clear();
		imageList = list;
	}

	public async void loadScrollViewImages(int left, int right) {
		if (oldLeft == left && oldRight == right) return;

		oldLeft = left;
		oldRight = right;
		stopCurrentLoadFunc?.Invoke();
		bool loading = true;
		stopCurrentLoadFunc = () => loading = false;

		List<ImageItem> items = [];
		if (this.imageList.Count <= MainViewModel.ImageMaxCacheCount) items.AddRange(this.imageList);
		else {
			var count = Math.Max((int)Math.Ceiling((MainViewModel.ImageMaxCacheCount - (right - left)) / 2), 1);
			int leftStart = Math.Max(0, left - count);
			int rightEnd = Math.Min(Math.Max(count - left, 0) + right + count, this.imageList.Count);

			items.AddRange(this.imageList[left..right]);
			items.AddRange(this.imageList[leftStart..left]);
			items.AddRange(this.imageList[right..rightEnd]);

			var itemDiff = imageList.Except(items);
			foreach (ImageItem img in itemDiff) {
				img.imageSource = null;
			}
		}

		var ratio = imgListPercentageRatio / 100;
		var containerWidth = (this.windowWidth - this.spaceOccupiedWidth) * ratio;
		var loadList = items.Where(p => p.imageSource == null);
		foreach (ImageItem item in loadList) {
			BitmapImage result = await ImageInfo.GetImageAsync(item.imagePath);
			if (!loading) return;

			var newHeight = result.Height * (containerWidth / result.Width);
			item.width = containerWidth;
			item.height = newHeight;
			item.originalWidth = result.Width;
			item.originalHeight = result.Height;
			item.imageSource = result;
		}
	}

	public void UpdateImgListSize(double percentage) {
		this.imgListPercentageRatio = percentage;
		var ratio = this.imgListPercentageRatio / 100;
		var w = (this.windowWidth - this.spaceOccupiedWidth) * ratio;

		foreach (ImageItem item in this.imageList) {
			item.width = w;
			item.height = item.originalHeight * (w / item.originalWidth);
		}
	}

	public void UpdateImgListSize(double w, double h) {
		if (this.imageList.Count == 0 || this.settings.fullScreen) return;

		this._windowWidth = w;
		this._windowHeight = h;
		this.UpdateImgListSize(this.imgListPercentageRatio);
	}
}

public class ImageItem : ViewModelBase {
	public required int index { get; init; }
	public required string imagePath { get; init; }
	public required double originalWidth { get; set; }
	public required double originalHeight { get; set; }

	private BitmapImage? _imageSource;
	private double _width;
	private double _height;

	public BitmapImage? imageSource {
		get => _imageSource;
		set {
			if (_imageSource == value) return;
			_imageSource = value;
			OnPropertyChanged();
		}
	}

	public double width {
		get => _width;
		set {
			if (value.Equals(_width)) return;
			_width = value;
			OnPropertyChanged();
		}
	}

	public double height {
		get => _height;
		set {
			if (value.Equals(_height)) return;
			_height = value;
			OnPropertyChanged();
		}
	}
}