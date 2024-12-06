using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using ImageViewer.Commands;
using ImageViewer.Models;
using Microsoft.Win32;
using Timer = System.Timers.Timer;

namespace ImageViewer.viewModels;

public partial class MainViewModel {
	private Dictionary<string, (int index, Task<ImageInfo> imageInfo)> imagePreloadList = new();
	private Action? stopCurrentLoadFunc;
	private Timer? slideshowTimer;

	public RelayCommand SaveAsCommand { get; }
	public RelayCommand PrevImageCommand { get; }
	public RelayCommand NextImageCommand { get; }
	public RelayCommand ToFirstImageCommand { get; }
	public RelayCommand ToLastImageCommand { get; }
	public RelayCommand ToggleSlideshowCommand { get; }
	public RelayCommand OpenFolderAndSelectFileCommand { get; }

	public string currentFilePath =>
		this.currentImagePathList.Length == 0 ? "" : this.currentImagePathList[this.currentImagePathIndex];
	/// 控件显示用 +1
	public int currentFilesIndex1 => this.currentFilePath == "" ? 0 : this._currentImagePathIndex + 1;

	public string _currentFileName = "";
	private ImageInfo _imageInfo = null!;
	private bool _isSlideshowActive;
	private string[] _currentImagePathList = [];
	private int _currentImagePathIndex;
	private bool _scrollToTop;

	public string currentFileName {
		get => _currentFileName;
		set {
			_currentFileName = value;
			OnPropertyChanged();
		}
	}
	public ImageInfo imageInfo {
		get => _imageInfo;
		set {
			_imageInfo = value;
			OnPropertyChanged();
		}
	}
	/// 幻灯片状态
	public bool isSlideshowActive {
		get => _isSlideshowActive;
		set {
			_isSlideshowActive = value;
			OnPropertyChanged();
		}
	}
	public string[] currentImagePathList {
		get => _currentImagePathList;
		set {
			_currentImagePathList = value;
			OnPropertyChanged();
		}
	}
	public int currentImagePathIndex {
		get => _currentImagePathIndex;
		set {
			_currentImagePathIndex = value;
			if (_currentImagePathList.Length > 0) currentFileName = _currentImagePathList[value];

			OnPropertyChanged();
			OnPropertyChanged(nameof(currentFilesIndex1));
			OnPropertyChanged(nameof(currentFilePath));
		}
	}

	public bool scrollToTop {
		get => _scrollToTop;
		set {
			_scrollToTop = value;
			OnPropertyChanged();
		}
	}

	public async Task OpenFileOrFolder(string path, bool isFile) {
		StopSlideshowTimer();
		stopCurrentLoadFunc?.Invoke();

		var dir = path;
		if (isFile) dir = Path.GetDirectoryName(path)!;
		var list = await MainViewModel.GetDirImageFilesAsync(dir);
		var imagePathIndex = isFile ? Array.FindIndex(list, v => v == path) : 0;

		await this.UpdateCurrentImagePathList(list, imagePathIndex);
	}

	public async Task OpenFile() {
		var openFolder = new OpenFileDialog {
			Filter = filterImgType,
			Multiselect = false
		};
		if (openFolder.ShowDialog() != true) return;

		await this.OpenFileOrFolder(openFolder.FileName, true);
	}

	public async Task OpenFolder() {
		var openFolder = new OpenFolderDialog {
			Multiselect = false
		};
		if (openFolder.ShowDialog() != true) return;

		await this.OpenFileOrFolder(openFolder.FolderName, true);
	}

	public async Task UpdateCurrentImagePathList(string[] list, int imagePathIndex) {
		if (list.Length == 0) {
			this.ResetSingleImage();
			this.ResetVerticalScroll();
			this.currentImagePathList = [];
			this.currentImagePathIndex = 0;
			return;
		}

		this.currentImagePathList = list;
		this.currentImagePathIndex = imagePathIndex;

		if (IsScrollViewMode()) {
			await this.InitScrollView(list);
			this.ResetSingleImage();
		} else {
			this.LoadImage(list[imagePathIndex]);
			this.ResetVerticalScroll();
		}
	}

	public void UpdateSlideshowInterval(int value) {
		if (this.settings.slideshowInterval == value) return;
		this.settingsManager.UpdateProperty(x => x.slideshowInterval, value);
		if (!isSlideshowActive) return;

		this.StopSlideshowTimer();
		this.ToggleSlideshow(null);
	}

	private bool CanToggleSlideshow(object? obj) {
		return !IsScrollViewMode() && CanNextImage(obj);
	}

	private void ToggleSlideshow(object? obj) {
		if (slideshowTimer == null) {
			slideshowTimer = new Timer(this.settings.slideshowInterval);
			slideshowTimer.Elapsed += SlideshowTimer_Elapsed;
			slideshowTimer.AutoReset = true;
			slideshowTimer.Enabled = true;
		}

		this.isSlideshowActive = !this.isSlideshowActive;
		if (isSlideshowActive) {
			slideshowTimer.Stop();
			slideshowTimer.Interval = this.settings.slideshowInterval;
			slideshowTimer.Start();
		} else {
			slideshowTimer.Stop();
		}
	}

	private void StopSlideshowTimer() {
		if (!isSlideshowActive) return;
		slideshowTimer?.Stop();
		this.isSlideshowActive = false;
	}

	private void SlideshowTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) {
		if (CanNextImage(null)) {
			NextImage(null);
		} else {
			this.ToggleSlideshow(null);
		}
	}

	private void ScrollToTop() {
		if (this.settings.singleToggleBackToTop) this.scrollToTop = true;
	}

	private async void ReLoadImage() {
		var path = this.imageInfo.filePath;
		var value = ImageInfo.FromAsync(path, rotation: imageRotationAngle);
		this.imagePreloadList[path] = (this.currentImagePathIndex, value);
		this.imageInfo = await value;
		this.ReScaleImage(null, null, true);
	}

	private async void LoadImage(string imagePath) {
		stopCurrentLoadFunc?.Invoke();
		this.ScrollToTop();

		var index = this.currentImagePathIndex;
		var curImgPath = this.imageInfo?.filePath;
		var curImg = imagePreloadList.TryGetValue(imagePath, out var value)
			? value
			: (index, ImageInfo.FromAsync(imagePath));
		imagePreloadList.Clear();
		imagePreloadList.Add(imagePath, curImg);

		if (curImgPath != null && curImgPath != imagePath) {
			var i = Array.FindIndex(this.currentImagePathList, p => p == curImgPath);
			imagePreloadList.Add(curImgPath, (i, Task.FromResult(this.imageInfo!)));
		}

		bool loading = true;
		stopCurrentLoadFunc = () => loading = false;
		ImageInfo result = await curImg.Item2;
		if (!loading) return;

		this.imageInfo = result;
		this.stopCurrentLoadFunc = null;
		this.ReScaleImage(null, null, true);
		if (index != this.currentImagePathList.Length - 1) {
			string prevPath = this.currentImagePathList[index + 1];
			if (!imagePreloadList.ContainsKey(prevPath))
				imagePreloadList.Add(prevPath, (index + 1, ImageInfo.FromAsync(prevPath)));
		}

		if (index > 0) {
			string nextPath = this.currentImagePathList[index - 1];
			if (!imagePreloadList.ContainsKey(nextPath))
				imagePreloadList.Add(nextPath, (index - 1, ImageInfo.FromAsync(nextPath)));
		}
	}

	private bool CanPrevImage(object? obj) {
		return this.currentImagePathList.Length > 1 && this.currentFilePath != this.currentImagePathList[0];
	}

	private void PrevImage(object? obj) {
		this.LoadImage(this.currentImagePathList[this.currentImagePathIndex -= 1]);
	}

	private bool CanNextImage(object? obj) {
		return this.currentImagePathList.Length > 1 && this.currentFilePath != this.currentImagePathList[^1];
	}

	private void NextImage(object? obj) {
		this.LoadImage(this.currentImagePathList[this.currentImagePathIndex += 1]);
	}

	private bool CanToFirstImage(object? obj) {
		return !this.IsScrollViewMode() && this.CanPrevImage(null);
	}

	private void ToFirstImage(object? obj) {
		this.currentImagePathIndex = 0;
		this.LoadImage(this.currentImagePathList[0]);
	}

	private bool CanToLastImage(object? obj) {
		return !this.IsScrollViewMode() && this.CanNextImage(null);
	}

	private void ToLastImage(object? obj) {
		this.currentImagePathIndex = this.currentImagePathList.Length - 1;
		this.LoadImage(this.currentImagePathList[^1]);
	}

	public void UpdateIndexAndImage(int index) {
		if (index < 0 || index >= this.currentImagePathList.Length || this.currentImagePathIndex == index) {
			OnPropertyChanged(nameof(currentFilesIndex1));
			return;
		}

		this.currentImagePathIndex = index;
		if (this.IsScrollViewMode()) return;
		this.LoadImage(this.currentImagePathList[index]);
	}

	private bool CanOpenFolderAndSelectFile(object? obj) {
		return this.currentImagePathList.Length > 0;
	}

	private void OpenFolderAndSelectFile(object? v) {
		if (v is not string path) return;

		IntPtr pidlList = ILCreateFromPathW(path);
		if (pidlList == IntPtr.Zero) return;
		try {
			Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(pidlList, 0, IntPtr.Zero, 0));
		} finally {
			ILFree(pidlList);
		}
	}

	private bool CanSaveAs(object? obj) {
		return this.currentFilePath != "" && !IsScrollViewMode();
	}

	private void SaveAs(object? obj) {
		var saveFile = new SaveFileDialog {
			FileName = this.currentFileName,
			DefaultExt = Regex.Match(this.currentFilePath, @"\.\w{3,4}$").Value,
			Filter = filterImgType
		};

		if (saveFile.ShowDialog() != true) return;
		var newPath = saveFile.FileName;
		try {
			File.Copy(this.currentFilePath, newPath, true);
		} catch (IOException) {
			MessageBox.Show("保存图片失败");
		}
	}

	private const string filterImgType = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";

	private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp",
		".ico", ".svg", ".heif", ".raw", ".psd", ".dds"
	};

	public static Task<string[]> GetDirImageFilesAsync(string dir) {
		return Task.Run(() => {
			string[] files = Directory.GetFiles(dir);
			var result = files.Where(IsImageFile).ToArray();
			Array.Sort(result, MainViewModel.StrCmpLogicalW);
			return result;
		});
	}

	public static bool IsImageFile(string filePath) {
		return ImageExtensions.Contains(Path.GetExtension(filePath).ToLower());
	}

	[LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
	private static partial int StrCmpLogicalW(string x, string y);

	[LibraryImport("shell32.dll")]
	private static partial void ILFree(IntPtr pidlList);

	[LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
	private static partial IntPtr ILCreateFromPathW(string pszPath);

	[LibraryImport("shell32.dll")]
	private static partial int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);
}