using System.Windows.Media;
using ImageViewer.utils;
using ImageViewer.viewModels;

namespace ImageViewer.Models;

public class Settings : ViewModelBase {
	private string defaultBackgroundText => "#ababab";
	public string backgroundText => isCustomBackground ? customBackgroundText : defaultBackgroundText;

	public bool singleToggleBackToTop { get; set; } = true;
	public string customBackgroundText { get; set; } = "#ababab";

	private bool _windowTopmost;
	private bool _fullScreenHidePointer;
	private bool _fullScreen;
	private bool _isStatusBarVisible = true;
	private int _slideshowInterval = 5000;
	private int _singleModelScrollSpeed = 10;
	private int _scrollModeScrollSpeed = 5;
	private double _imgPercentageRatio = 100.0;
	private bool _isCustomBackground;
	private Enums.EViewMode _currentViewMode = Enums.EViewMode.SingleImage;
	private Enums.EScaleType _scaleType = Enums.EScaleType.FitImage;

	public bool windowTopmost {
		get => _windowTopmost;
		set {
			_windowTopmost = value;
			OnPropertyChanged();
		}
	}
	public bool fullScreenHidePointer {
		get => _fullScreenHidePointer;
		set {
			_fullScreenHidePointer = value;
			OnPropertyChanged();
		}
	}
	[WpfAppConfig.DoNotSave]
	public bool fullScreen {
		get => _fullScreen;
		set {
			_fullScreen = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(isStatusBarVisible));
		}
	}
	public bool isStatusBarVisible {
		get => this._isStatusBarVisible;
		set {
			_isStatusBarVisible = value;
			OnPropertyChanged();
		}
	}
	public int slideshowInterval {
		get => _slideshowInterval;
		set {
			var v = value switch {
				> 60000 => 60000,
				< 100 => 100,
				_ => value
			};
			_slideshowInterval = v;
			OnPropertyChanged();
		}
	}
	public int scrollModeScrollSpeed {
		get => _scrollModeScrollSpeed;
		set {
			_scrollModeScrollSpeed = value;
			OnPropertyChanged();
		}
	}
	public int singleModelScrollSpeed {
		get => _singleModelScrollSpeed;
		set {
			_singleModelScrollSpeed = value;
			OnPropertyChanged();
		}
	}
	public double imgPercentageRatio {
		get => _imgPercentageRatio;
		set {
			value = value switch {
				> 1000 => 1000,
				< 1 => 1,
				_ => value
			};
			_imgPercentageRatio = value;
			OnPropertyChanged();
		}
	}
	public Enums.EViewMode currentViewMode {
		get => _currentViewMode;
		set {
			if (value == _currentViewMode) return;
			_currentViewMode = value;
			OnPropertyChanged();
		}
	}
	public Enums.EScaleType scaleType {
		get => _scaleType;
		set {
			if (value == _scaleType) return;
			_scaleType = value;
			OnPropertyChanged();
		}
	}
	public bool isCustomBackground {
		get => _isCustomBackground;
		set {
			_isCustomBackground = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(backgroundText));
		}
	}
}