using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using ImageViewer.Commands;
using ImageViewer.Models;
using ImageViewer.Services;

namespace ImageViewer.viewModels;

public partial class MainViewModel : ViewModelBase {
	private SettingsManager settingsManager { get; }
	public Settings settings => settingsManager.settings;
	public bool isCodeResizing { get; private set; }
	/// 控件占用空间
	public int menuHeight => 22;
	public int statusHeight => this.settings.isStatusBarVisible && !this.settings.fullScreen ? 21 : 0;
	public int imageContainerMargin => 1;
	/// 窗口最小高宽
	public int windowMinWidth => 400;
	public int windowMinHeight => 200;
	/// 窗口占用大小
	private double _spaceOccupiedWidth { get; set; }
	private double _spaceOccupiedHeight { get; set; }
	/// 窗口合计占用
	private double imgMargin => imageContainerMargin * 2;
	private double spaceOccupiedWidth => this.settings.fullScreen ? 0 : imgMargin + _spaceOccupiedWidth;
	private double spaceOccupiedHeight =>
		this.settings.fullScreen ? 0 : menuHeight + statusHeight + imgMargin + _spaceOccupiedHeight;
	private Rotation imageRotationAngle;

	private double _windowLeft = (SystemParameters.WorkArea.Size.Width - 1200) / 2;
	private double _windowTop = (SystemParameters.WorkArea.Size.Height - 720) / 2;
	private double _windowWidth = 1200;
	private double _windowHeight = 720;

	public double windowLeft {
		get => _windowLeft;
		set {
			_windowLeft = value;
			OnPropertyChanged();
		}
	}
	public double windowTop {
		get => _windowTop;
		set {
			_windowTop = value;
			OnPropertyChanged();
		}
	}
	public double windowWidth {
		get => _windowWidth;
		set {
			_windowWidth = value;
			OnPropertyChanged();
		}
	}
	public double windowHeight {
		get => _windowHeight;
		set {
			_windowHeight = value;
			OnPropertyChanged();
		}
	}

	public RelayCommand ToggleStatusBarVisibleCommand { get; }
	public RelayCommand SingleImageModeCommand { get; }
	public RelayCommand VerticalScrollModeCommand { get; }
	public RelayCommand FullScreenCommand { get; }

	public MainViewModel(SettingsManager settingsManager) {
		this.settingsManager = settingsManager;
		settingsManager.settings.PropertyChanged += Settings_PropertyChanged;

		this.SaveAsCommand = new RelayCommand(SaveAs, CanSaveAs);
		this.PrevImageCommand = new RelayCommand((e) => {
			StopSlideshowTimer();
			PrevImage(null);
		}, CanPrevImage);
		this.NextImageCommand = new RelayCommand((e) => {
			StopSlideshowTimer();
			NextImage(null);
		}, CanNextImage);
		this.ToFirstImageCommand = new RelayCommand(ToFirstImage, CanToFirstImage);
		this.ToLastImageCommand = new RelayCommand(ToLastImage, CanToLastImage);
		this.ChangeScaleTypeCommand = new RelayCommand(ChangeScaleType, CanChangeScaleType);
		this.ZoomImgScaleCommand = new RelayCommand(AdjustImgScale, CanZoomImgScale);
		this.MinifyImgScaleCommand = new RelayCommand(AdjustImgScale, CanMinifyImgScale);
		this.ToggleSlideshowCommand = new RelayCommand(ToggleSlideshow, CanToggleSlideshow);
		this.OpenFolderAndSelectFileCommand = new RelayCommand(OpenFolderAndSelectFile);
		this.ImageRotationAngleCommand = new RelayCommand(ImageRotationAngle, CanImageRotationAngle);
		this.ToggleStatusBarVisibleCommand = new RelayCommand(ToggleStatusBarVisible);
		this.SingleImageModeCommand = new RelayCommand(_ => ToggleViewMode(Enums.EViewMode.SingleImage),
			_ => CanToggleViewMode(Enums.EViewMode.SingleImage));
		this.VerticalScrollModeCommand = new RelayCommand(_ => ToggleViewMode(Enums.EViewMode.VerticalScroll),
			_ => CanToggleViewMode(Enums.EViewMode.VerticalScroll));
		this.FullScreenCommand = new(this.ToggleFullScreen);
	}

	private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(this.settings.currentViewMode)) {
			ToggleViewMode(this.settings.currentViewMode);
		}
	}

	public bool IsScrollViewMode() {
		return this.settings.currentViewMode != Enums.EViewMode.SingleImage;
	}

	public bool IsImageInfo() {
		return this.imageInfo != null;
	}

	public void initWindowSpaceOccupied(double spaceOccupiedW, double spaceOccupiedH) {
		_spaceOccupiedWidth = spaceOccupiedW;
		_spaceOccupiedHeight = spaceOccupiedH;
	}

	public void ToggleFullScreen(object? obb) {
		this.settingsManager.UpdateProperty(x => x.fullScreen, !this.settings.fullScreen);
	}

	private bool CanToggleViewMode(Enums.EViewMode mode) {
		return this.settings.currentViewMode != mode;
	}

	private void ToggleViewMode(Enums.EViewMode mode) {
		StopSlideshowTimer();
		this.settingsManager.UpdateProperty(x => x.currentViewMode, mode);
		ResetViewModel();
	}

	private void ToggleStatusBarVisible(object? obj) {
		this.settingsManager.UpdateProperty(x => x.isStatusBarVisible, !this.settings.isStatusBarVisible);
		this.ReScaleImage(null, null, true);
	}

	private bool CanImageRotationAngle(object? obj) {
		return !IsScrollViewMode() && IsImageInfo();
	}

	private void ImageRotationAngle(object? isLeft) {
		StopSlideshowTimer();
		imageRotationAngle = (Rotation)(((int)imageRotationAngle + (isLeft != null && (bool)isLeft ? -1 : 1) + 4) % 4);
		ReLoadImage();
	}

	private async void ResetViewModel() {
		stopCurrentLoadFunc?.Invoke();

		if (IsScrollViewMode()) {
			if (this._imageInfo != null) {
				var value = this.imagePreloadList.Values;
				var results =
					await Task.WhenAll(value.Select(async item => (item.index, img: await item.imageInfo)));

				await InitScrollView(this._currentImagePathList, results);
			}

			this.ResetSingleImage();
			return;
		}

		if (this.currentImagePathList.Length > 0) {
			this.LoadImage(this.currentImagePathList[this.currentImagePathIndex]);
		}

		this.ResetVerticalScroll();
	}

	private void ResetSingleImage() {
		this.imageInfo = null!;
		this.slideshowTimer = null;
		this.imagePreloadList.Clear();
		imageRotationAngle = Rotation.Rotate0;
	}

	private void ResetVerticalScroll() {
		this.imageList.Clear();
	}

	private void UpdateWindowPos() {
		this.windowLeft = (SystemParameters.WorkArea.Width - Math.Max(this.windowWidth, windowMinWidth)) / 2;
		this.windowTop = (SystemParameters.WorkArea.Height - Math.Max(this.windowHeight, windowMinHeight)) / 2;
	}
}