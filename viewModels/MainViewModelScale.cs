using System.Windows;
using ImageViewer.Commands;
using ImageViewer.Models;

namespace ImageViewer.viewModels;

public partial class MainViewModel {
	public RelayCommand ChangeScaleTypeCommand { get; }
	public RelayCommand ZoomImgScaleCommand { get; }
	public RelayCommand MinifyImgScaleCommand { get; }
	public RelayCommand ImageRotationAngleCommand { get; }

	private double _imgWidth = 200;
	private double _imgHeight = 200;

	public double imgWidth {
		get => _imgWidth;
		set {
			_imgWidth = value;
			OnPropertyChanged();
		}
	}
	public double imgHeight {
		get => _imgHeight;
		set {
			_imgHeight = value;
			OnPropertyChanged();
		}
	}

	public void ReScaleImage(double? aWidth = null, double? aHeight = null, bool reSizeWindow = false) {
		if (!this.IsImageInfo()) return;

		if (this.settings.scaleType == Enums.EScaleType.Percentage) {
			var (width, height) = CalcImgSize();
			if (Math.Abs(this.imgWidth - width) < 1e-10 && Math.Abs(this.imgHeight - height) < 1e-10) return;
			this.UpdateImgPercentageRatio(this.settings.imgPercentageRatio);
		} else {
			var (w, h) = this.settings.scaleType switch {
				Enums.EScaleType.FitHeight => this.FitHeight(aHeight),
				Enums.EScaleType.FitWidth => this.FitWidth(aWidth),
				Enums.EScaleType.FitImage => this.FitImage(),
				_ => this.FullSize()
			};
			if (reSizeWindow) UpdateWindowSize(w, h);
			this.UpdateImgPercentageRatio();
		}
	}

	public void UpdateImgPercentageRatio(double PercentageRatio) {
		this.settingsManager.UpdateProperty(x => x.imgPercentageRatio, PercentageRatio);
		if (!this.IsImageInfo()) return;

		this.UpdateCurrentImgSize();
		var windowW = Math.Min(SystemParameters.WorkArea.Size.Width - spaceOccupiedWidth, this.imgWidth);
		var windowH = Math.Min(SystemParameters.WorkArea.Size.Height - spaceOccupiedHeight, this.imgHeight);
		this.UpdateWindowSize(windowW, windowH);

		if (this.settings is { scaleType : Enums.EScaleType.FitImage, currentViewMode: Enums.EViewMode.SingleImage }) {
			this.settingsManager.UpdateProperty(x => x.scaleType, Enums.EScaleType.Percentage);
		}
	}

	private void UpdateImgPercentageRatio(double? newImgW = null, double? newImgH = null) {
		newImgW ??= this.imgWidth;
		newImgH ??= this.imgHeight;
		double imageWidth = this.imageInfo.width;
		double imageHeight = this.imageInfo.height;

		double widthRatio = (double)(newImgW / imageWidth);
		double heightRatio = (double)(newImgH / imageHeight);
		this.settingsManager.UpdateProperty(x => x.imgPercentageRatio, Math.Min(widthRatio, heightRatio) * 100);
	}

	private bool CanChangeScaleType(object? value) {
		var v = (Enums.EScaleType)value!;
		return this.settings.scaleType != v;
	}

	private void ChangeScaleType(object? value) {
		var v = (Enums.EScaleType)value!;
		this.settingsManager.UpdateProperty(x => x.scaleType, v);

		this.ReScaleImage(null, null, true);
	}

	private (double, double) FullSize() {
		double imageWidth = this.imageInfo.width;
		double imageHeight = this.imageInfo.height;

		var windowW = Math.Min(SystemParameters.WorkArea.Size.Width - spaceOccupiedWidth, imageWidth);
		var windowH = Math.Min(SystemParameters.WorkArea.Size.Height - spaceOccupiedHeight, imageHeight);
		this.UpdateCurrentImgSize(imageWidth, imageHeight);
		return (windowW, windowH);
	}

	private (double, double) FitImage() {
		double imageWidth = this.imageInfo.width;
		double imageHeight = this.imageInfo.height;
		double availableWidth = SystemParameters.WorkArea.Size.Width - spaceOccupiedWidth;
		double availableHeight = SystemParameters.WorkArea.Size.Height - spaceOccupiedHeight;

		double imgR = imageWidth / imageHeight;
		double availableR = availableWidth / availableHeight;
		double newW = imageWidth, newH = imageHeight;
		if (imgR >= availableR && imageWidth > availableWidth) {
			newW = availableWidth;
			newH = imageHeight * availableWidth / imageWidth;
		} else if (imageHeight > availableHeight) {
			newW = imageWidth * availableHeight / imageHeight;
			newH = availableHeight;
		}

		this.UpdateCurrentImgSize(newW, newH);
		return (newW, newH);
	}

	private (double, double) FitWidth(double? newAvailableWidth) {
		double imageWidth = this.imageInfo.width;
		double imageHeight = this.imageInfo.height;
		double availableWidth = (newAvailableWidth ?? SystemParameters.WorkArea.Size.Width) - spaceOccupiedWidth;
		double availableHeight = SystemParameters.WorkArea.Size.Height - spaceOccupiedHeight;
		double newHeight = imageHeight * availableWidth / imageWidth;

		var windowW = availableWidth;
		var windowH = Math.Min(newHeight, availableHeight);
		this.UpdateCurrentImgSize(availableWidth, newHeight);
		return (windowW, windowH);
	}


	private (double, double) FitHeight(double? newAvailableHeight) {
		double imageWidth = this.imageInfo.width;
		double imageHeight = this.imageInfo.height;
		double availableWidth = SystemParameters.WorkArea.Size.Width - spaceOccupiedWidth;
		double availableHeight = (newAvailableHeight ?? SystemParameters.WorkArea.Size.Height) - spaceOccupiedHeight;
		double newWidth = availableHeight * (imageWidth / imageHeight);

		var windowW = Math.Min(newWidth, availableWidth);
		var windowH = availableHeight;
		this.UpdateCurrentImgSize(newWidth, availableHeight);
		return (windowW, windowH);
	}

	private void AdjustImgScale(object? isZoomIn) {
		double scaleStep = 10.0;
		if (!(bool)isZoomIn!) scaleStep = -scaleStep;

		var newScale = this.settings.imgPercentageRatio < 10.0 ? scaleStep : this.settings.imgPercentageRatio + scaleStep;
		this.UpdateImgPercentageRatio(Math.Clamp(newScale, 1, 1000));
	}

	private bool CanZoomImgScale(object? value) {
		return this.settings.imgPercentageRatio < 1000.0;
	}

	private bool CanMinifyImgScale(object? value) {
		return this.settings.imgPercentageRatio > 1.0;
	}

	private void UpdateCurrentImgSize() {
		var (width, height) = CalcImgSize();
		this.UpdateCurrentImgSize(width, height);
	}

	private void UpdateCurrentImgSize(double w, double h) {
		this.imgWidth = w;
		this.imgHeight = h;
	}

	private void UpdateWindowSize(double w, double h) {
		isCodeResizing = true;
		this.windowWidth = w + spaceOccupiedWidth;
		this.windowHeight = h + spaceOccupiedHeight;
		this.UpdateWindowPos();
		isCodeResizing = false;
	}

	private (double, double) CalcImgSize() {
		double imageWidth = this.imageInfo.width;
		double imageHeight = this.imageInfo.height;
		double w = imageWidth * (this.settings.imgPercentageRatio / 100);
		double h = imageHeight * (this.settings.imgPercentageRatio / 100);
		return (w, h);
	}
}