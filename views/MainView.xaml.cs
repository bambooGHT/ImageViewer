using System.ComponentModel;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ImageViewer.Commands;
using ImageViewer.Models;
using ImageViewer.utils;
using ImageViewer.viewModels;

namespace ImageViewer.views;

public partial class MainView {
	private bool toExitFullScreen;
	private WindowState previousWindowState;
	private WindowStyle previousWindowStyle;
	private Action? scrollViewDebounce;
	private EventHandler<RenderingEventArgs>? scrollViewScrollHandler;
	private ScrollViewer currentScrollViewer = null!;
	private Dictionary<Key, bool> keyDict = new() {
		[Key.Down] = false,
		[Key.Up] = false,
		[Key.Left] = false,
		[Key.Right] = false
	};
	public MainViewModel viewModel { get; init; }

	public RelayCommand OpenFileCommand { get; }
	public RelayCommand OpenFolderCommand { get; }
	public RelayCommand ShowPropsCommand { get; }
	public RelayCommand ScrollViewRangeScrollCommand { get; }

	public MainView() {
		this.OpenFileCommand = new RelayCommand(OpenFile);
		this.OpenFolderCommand = new RelayCommand(OpenFolder);
		this.ShowPropsCommand = new(this.ShowProps);
		this.ScrollViewRangeScrollCommand = new(this.ScrollViewControl_RangeScroll);

		InitializeComponent();
		this.viewModel = new MainViewModel(App.settingsManager);
		this.DataContext = this.viewModel;
		this.BindKeys();
		this.KeyUp += ScrollViewer_KeyUp;
		this.KeyDown += ScrollViewer_KeyDown;
		this.PreviewKeyDown += ScrollViewer_PreviewKeyDown;
		this.PreviewMouseWheel += ScrollWheelUpdateScaleImage;
		this.Closing += (t, e) => {
			App.settingsManager.SaveSettings();
		};
		this.viewModel.settings.PropertyChanged += Settings_PropertyChanged;
		AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
			App.settingsManager.SaveSettings();
		};
	}

	private void BindKeys() {
		InputBindings.Add(new KeyBinding(this.OpenFileCommand, Key.O, ModifierKeys.Control));
		InputBindings.Add(new KeyBinding(this.OpenFolderCommand, Key.F2, ModifierKeys.Control));
		InputBindings.Add(new KeyBinding(viewModel.SaveAsCommand, Key.S, ModifierKeys.Control));
		InputBindings.Add(new KeyBinding(ApplicationCommands.Close, Key.X, ModifierKeys.Alt));
		InputBindings.Add(new KeyBinding { Key = Key.F, Command = viewModel.FullScreenCommand });
		InputBindings.Add(
			new KeyBinding { Key = Key.Add, Command = viewModel.ZoomImgScaleCommand, CommandParameter = true });
		InputBindings.Add(new KeyBinding
			{ Key = Key.Subtract, Command = viewModel.MinifyImgScaleCommand, CommandParameter = false });
		InputBindings.Add(new KeyBinding {
			Key = Key.Divide,
			Command = viewModel.ChangeScaleTypeCommand, CommandParameter = Enums.EScaleType.FullSize
		});
		InputBindings.Add(new KeyBinding {
			Key = Key.Multiply,
			Command = viewModel.ChangeScaleTypeCommand, CommandParameter = Enums.EScaleType.FitImage
		});
		InputBindings.Add(new KeyBinding {
			Key = Key.Right, Modifiers = ModifierKeys.Alt,
			Command = viewModel.ChangeScaleTypeCommand, CommandParameter = Enums.EScaleType.FitWidth
		});
		InputBindings.Add(new KeyBinding {
			Key = Key.Down, Modifiers = ModifierKeys.Alt,
			Command = viewModel.ChangeScaleTypeCommand, CommandParameter = Enums.EScaleType.FitHeight
		});
		InputBindings.Add(new KeyBinding {
			Key = Key.Divide, Modifiers = ModifierKeys.Control | ModifierKeys.Alt,
			Command = viewModel.ChangeScaleTypeCommand, CommandParameter = Enums.EScaleType.Percentage
		});
		InputBindings.Add(new KeyBinding { Key = Key.Pause, Command = viewModel.ToggleSlideshowCommand });
		InputBindings.Add(new KeyBinding {
			Key = Key.Left, Modifiers = ModifierKeys.Control, Command = viewModel.ImageRotationAngleCommand,
			CommandParameter = true
		});
		InputBindings.Add(new KeyBinding {
			Key = Key.Right, Modifiers = ModifierKeys.Control, Command = viewModel.ImageRotationAngleCommand,
			CommandParameter = false
		});
		InputBindings.Add(new KeyBinding { Key = Key.F7, Command = viewModel.SingleImageModeCommand });
		InputBindings.Add(new KeyBinding { Key = Key.F8, Command = viewModel.VerticalScrollModeCommand });
		InputBindings.Add(new KeyBinding { Key = Key.Home, Command = viewModel.ToFirstImageCommand });
		InputBindings.Add(new KeyBinding { Key = Key.End, Command = viewModel.ToLastImageCommand });
		InputBindings.Add(new KeyBinding { Key = Key.PageDown });
		InputBindings.Add(new KeyBinding { Key = Key.PageUp });
		ViewModeShortcutKey(this.viewModel.settings.currentViewMode);
	}

	private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (e.PropertyName == nameof(this.viewModel.settings.currentViewMode)) {
			imgControlContainer.Dispatcher.InvokeAsync(() => {
				currentScrollViewer = FindVisualChild<ScrollViewer>(imgControlContainer)!;
				ViewModeShortcutKey(this.viewModel.settings.currentViewMode);
			}, DispatcherPriority.Loaded);
		} else if (e.PropertyName == nameof(this.viewModel.settings.fullScreen)) {
			if (this.viewModel.settings.fullScreen) ToFullScreen();
			else ExitFullScreen();
		}
	}

	private void ViewModeShortcutKey(Enums.EViewMode value) {
		KeyBinding pageDownBinding = InputBindings.OfType<KeyBinding>().FirstOrDefault(b => b.Key == Key.PageDown)!;
		KeyBinding pageUpBinding = InputBindings.OfType<KeyBinding>().FirstOrDefault(b => b.Key == Key.PageUp)!;

		if (value == Enums.EViewMode.VerticalScroll) {
			pageDownBinding.Command = this.ScrollViewRangeScrollCommand;
			pageDownBinding.CommandParameter = "next";
			pageUpBinding.Command = this.ScrollViewRangeScrollCommand;
			pageUpBinding.CommandParameter = "prev";
			scrollViewDebounce = new Debounce().Create(() => ScrollViewControl_DelayLoadImages(null!, null!),
				TimeSpan.FromMilliseconds(350));
			if (this.viewModel.imageList.Count > 0) {
				this.ScrollViewerToIndexView(this.viewModel.currentImagePathIndex);
			}

			return;
		}

		pageDownBinding.Command = this.viewModel.NextImageCommand;
		pageDownBinding.CommandParameter = null;
		pageUpBinding.Command = this.viewModel.PrevImageCommand;
		pageUpBinding.CommandParameter = null;
		scrollViewDebounce = null;
	}

	private async void ShowProps(object? t) {
		ImageInfo value = viewModel.imageInfo;
		if (t == null) return;

		if (t is not ImageInfo v) {
			v = await ImageInfo.FromAsync((string)t, false);
		}

		new FileProps(v) {
			Owner = this,
			WindowStartupLocation = WindowStartupLocation.CenterOwner
		}.ShowDialog();
	}

	private async void OpenFile(object? obj) {
		await this.viewModel.OpenFile();
		this.UpdateScrollOffset();
	}

	private async void OpenFolder(object? obj) {
		await this.viewModel.OpenFolder();
		this.UpdateScrollOffset();
	}

	private void ScrollViewControl_RangeScroll(object? obj) {
		var scrollViewer = FindVisualChild<ScrollViewer>(imgControlContainer)!;
		var value = scrollViewer.ViewportHeight - 40;
		if ((string)obj! == "prev") value = -value;

		scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + value);
	}

	private void ToFullScreen() {
		if (this.viewModel.settings.fullScreenHidePointer) Mouse.OverrideCursor = Cursors.None;

		previousWindowState = this.WindowState;
		previousWindowStyle = this.WindowStyle;
		this.WindowStyle = WindowStyle.None;
		this.WindowState = WindowState.Maximized;
		this.ResizeMode = ResizeMode.NoResize;
	}

	private void ExitFullScreen() {
		Mouse.OverrideCursor = null;

		this.WindowState = previousWindowState;
		this.WindowStyle = previousWindowStyle;
		this.ResizeMode = ResizeMode.CanResize;
	}

	private void MainView_OnLoaded(object sender, RoutedEventArgs e) {
		this.viewModel.initWindowSpaceOccupied(this.ActualWidth - Box.ActualWidth, this.ActualHeight - Box.ActualHeight);
		imgControlContainer.Dispatcher.InvokeAsync(() => {
			currentScrollViewer = FindVisualChild<ScrollViewer>(imgControlContainer)!;
		}, DispatcherPriority.Loaded);
		imgControlContainer.ContentTemplateSelector = new ModeTemplateSelector<Enums.EViewMode>(
			new() {
				{ Enums.EViewMode.SingleImage, (DataTemplate)FindResource("imgSingle") },
				{ Enums.EViewMode.VerticalScroll, (DataTemplate)FindResource("imgScroll") }
			});
		statusControl.ContentTemplateSelector = new ModeTemplateSelector<Enums.EViewMode>(new() {
			{ Enums.EViewMode.SingleImage, (DataTemplate)FindResource("singleImageStatusTemp") },
			{ Enums.EViewMode.VerticalScroll, (DataTemplate)FindResource("verticalScrollStatusTemp") }
		});
	}

	private void MainView_OnSizeChanged(object sender, SizeChangedEventArgs e) {
		if (this.viewModel.isCodeResizing) return;
		if (this.viewModel.IsScrollViewMode()) {
			this.viewModel.UpdateImgListSize(ActualWidth, ActualHeight);
			return;
		}

		if (this.WindowState == WindowState.Maximized) {
			toExitFullScreen = true;
			this.viewModel.ReScaleImage(Box.ActualWidth, Box.ActualHeight);
		} else {
			this.viewModel.ReScaleImage(ActualWidth, ActualHeight, toExitFullScreen);
			if (toExitFullScreen) toExitFullScreen = false;
		}
	}

	private void UpdateImgPercentage_LostFocus(object sender, RoutedEventArgs e) {
		var t = (TextBox)sender;
		var value = double.Parse(t.Text);
		if (this.viewModel.IsScrollViewMode()) {
			this.viewModel.UpdateImgListSize(value);
			return;
		}

		this.viewModel.UpdateImgPercentageRatio(value);
	}

	private void UpdateImgPercentage_KeyDown(object sender, KeyEventArgs e) {
		if (e.Key != Key.Enter) return;
		e.Handled = true;

		UpdateImgPercentage_LostFocus(sender, e);
		FocusManager.SetFocusedElement(this, this);
	}

	private void ToggleImgIndex_LostFocus(object sender, RoutedEventArgs e) {
		var t = (TextBox)sender;
		if (!int.TryParse(t.Text, out int value)) value = this.viewModel.currentFilesIndex1;

		this.viewModel.UpdateIndexAndImage(value - 1);
		if (this.viewModel.IsScrollViewMode()) ScrollViewerToIndexView(value - 1);
	}

	private void ToggleImgIndex_KeyDown(object sender, KeyEventArgs e) {
		if (e.Key != Key.Enter) return;
		e.Handled = true;

		ToggleImgIndex_LostFocus(sender, e);
		FocusManager.SetFocusedElement(this, this);
	}

	private void UpdateSlideshowInterval(object sender, RoutedEventArgs e) {
		var inputDialog = new InputDialog(this.viewModel.settings.slideshowInterval) {
			Owner = this,
			WindowStartupLocation = WindowStartupLocation.CenterOwner
		};
		bool? result = inputDialog.ShowDialog();

		if (result != true) return;
		int inputValue = inputDialog.InputValue;
		this.viewModel.UpdateSlideshowInterval(inputValue);
	}

	private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
		e.Handled = true;
	}

	/// <summary>
	/// 鼠标左键按住滚动图片
	/// </summary>
	private void ScrollViewer_MouseLeftDown(object sender, MouseButtonEventArgs e) {
		var t = sender as ScrollViewer;
		if (t is null or { ScrollableWidth: 0, ScrollableHeight: 0 }) return;

		this.Cursor = Cursors.ScrollAll;
		this.CaptureMouse();

		Point mouseStartPos = e.GetPosition(null);

		this.MouseMove += MouseMove;
		this.MouseLeftButtonUp += MouseUp;
		return;

		void MouseUp(object sender2, MouseButtonEventArgs e2) {
			this.Cursor = Cursors.Arrow;
			this.MouseMove -= MouseMove;
			this.MouseLeftButtonUp -= MouseUp;
			this.ReleaseMouseCapture();
		}

		void MouseMove(object sender1, MouseEventArgs e1) {
			Point currentPos = e1.GetPosition(null);
			Vector delta = mouseStartPos - currentPos;
			t.ScrollToHorizontalOffset(t.HorizontalOffset + delta.X);
			t.ScrollToVerticalOffset(t.VerticalOffset + delta.Y);
			mouseStartPos = currentPos;
		}
	}

	private void ScrollViewControl_OnScrollChanged(object sender, ScrollChangedEventArgs e) {
		scrollViewDebounce?.Invoke();
	}

	private EventHandler<RenderingEventArgs> ScrollViewControl_Scroll() {
		var offset = this.viewModel.IsScrollViewMode()
			? viewModel.settings.scrollModeScrollSpeed
			: viewModel.settings.singleModelScrollSpeed;

		return (_, _) => {
			double verticalOffset = currentScrollViewer.VerticalOffset;
			double horizontalOffset = currentScrollViewer.HorizontalOffset;

			if (keyDict[Key.Down]) verticalOffset += offset;
			else if (keyDict[Key.Up]) verticalOffset -= offset;

			if (keyDict[Key.Right]) horizontalOffset += offset;
			else if (keyDict[Key.Left]) horizontalOffset -= offset;

			currentScrollViewer.ScrollToVerticalOffset(verticalOffset);
			currentScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
		};
	}

	private void ScrollWheelUpdateScaleImage(object sender, MouseWheelEventArgs e) {
		e.Handled = true;
		if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
			var v = e.Delta > 0 ? 2 : -2;
			if (this.viewModel.IsScrollViewMode()) {
				this.viewModel.UpdateImgListSize(this.viewModel.imgListPercentageRatio + v);
			} else this.viewModel.UpdateImgPercentageRatio(this.viewModel.settings.imgPercentageRatio + v);
		}
	}

	private void ScrollViewControl_DelayLoadImages(object? sender, ElapsedEventArgs e) {
		Application.Current.Dispatcher.Invoke(() => {
			var scrollViewer = FindVisualChild<ScrollViewer>(imgControlContainer)!;
			var virtualizingStackPanel = FindVisualChild<VirtualizingStackPanel>(scrollViewer)!;
			UIElementCollection list = virtualizingStackPanel.Children;
			if (list.Count == 0) return;

			var middleIndex = 0;
			var viewportHeight = virtualizingStackPanel.ViewportHeight;
			var viewportCenter = viewportHeight / 2;
			List<int> indexList = [];

			for (var index = 0; index < list.Count; index++) {
				var item = (ListBoxItem)list[index];
				GeneralTransform transform = item.TransformToAncestor(virtualizingStackPanel);

				double y = transform.Transform(new(0, 0)).Y;
				var itemOccupancy = y + item.ActualHeight;
				if (!(itemOccupancy >= 0) || !(y <= viewportHeight)) continue;

				var v = ((ImageItem)item.Content).index;
				indexList.Add(v);
				if (y < viewportCenter && itemOccupancy >= viewportCenter) middleIndex = v;
			}

			if (indexList.Count == 0) return;

			this.viewModel.UpdateIndexAndImage(middleIndex - 1);
			this.viewModel.loadScrollViewImages(indexList[0] - 1, indexList[^1]);
		});
	}

	private void UpdateScrollOffset() {
		var t = FindVisualChild<ScrollViewer>(imgControlContainer);
		if (t == null) return;

		t.ScrollToHorizontalOffset(0);
		if (this.viewModel.IsScrollViewMode() && this.viewModel.imageList.Count > 0)
			this.ScrollViewerToIndexView(this.viewModel.currentImagePathIndex);
		else t.ScrollToVerticalOffset(0);
	}

	private void ScrollViewerToIndexView(int index) {
		var imgList = FindVisualChild<ListBox>(imgControlContainer, "imgList")!;
		imgList.ScrollIntoView(imgList.Items[index]!);
	}

	private void ScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e) {
		e.Handled = e.IsRepeat;
	}

	private void OpenSettingsView(object sender, RoutedEventArgs e) {
		new SettingsView(App.settingsManager) {
			Owner = this,
			WindowStartupLocation = WindowStartupLocation.CenterOwner
		}.ShowDialog();
	}

	private void Box_OnDragOver(object sender, DragEventArgs e) {
		var effect = DragDropEffects.None;
		if (e.Data.GetDataPresent(DataFormats.FileDrop)
		    && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files) {
			string path = files[0];
			if (Directory.Exists(path) || MainViewModel.IsImageFile(path)) {
				effect = DragDropEffects.Copy;
			}
		}

		e.Effects = effect;
		e.Handled = true;
	}

	private async void Box_OnDrop(object sender, DragEventArgs e) {
		var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
		await this.viewModel.OpenFileOrFolder(files[0], File.Exists(files[0]));
	}

	/// <summary>
	/// 箭头按键滚动图片
	/// </summary>
	private void ScrollViewer_KeyDown(object sender, KeyEventArgs e) {
		if (!keyDict.ContainsKey(e.Key)) return;

		keyDict[e.Key] = true;
		if (scrollViewScrollHandler != null) return;
		scrollViewScrollHandler = ScrollViewControl_Scroll();
		CompositionTargetEx.FrameUpdating += scrollViewScrollHandler;
	}

	private void ScrollViewer_KeyUp(object sender, KeyEventArgs e) {
		if (!keyDict.ContainsKey(e.Key)) return;
		keyDict[e.Key] = false;

		if (keyDict.Values.Any(v => v)) return;
		CompositionTargetEx.FrameUpdating -= scrollViewScrollHandler;
		scrollViewScrollHandler = null;
	}

	/// <summary>
	/// 点击空白处取消聚焦
	/// </summary>
	protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
		if (Keyboard.FocusedElement is UIElement { IsMouseOver: false }) {
			FocusManager.SetFocusedElement(this, this);
		}

		base.OnPreviewMouseDown(e);
	}

	/// <summary>
	/// 查找子控件
	/// </summary>
	/// <param name="parent">查找的子控件的父控件</param>
	/// <param name="name">(可选)子控件的Name</param>
	/// <typeparam name="T">子控件类型</typeparam>
	public static T? FindVisualChild<T>(DependencyObject parent, string? name = null) where T : FrameworkElement {
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is T childType && childType.Name == name | string.IsNullOrEmpty(name)) {
				return childType;
			}

			var childOfChild = FindVisualChild<T>(child, name);
			if (childOfChild != null) return childOfChild;
		}

		return null;
	}

	/// <summary>  
	/// 获得指定元素的所有子元素  
	/// </summary>
	public static List<T> GetChildObjects<T>(DependencyObject obj, bool recursion) where T : FrameworkElement {
		if (obj == null) return [];

		List<T> childList = [];
		for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++) {
			DependencyObject child = VisualTreeHelper.GetChild(obj, i);
			if (child is T element) {
				childList.Add(element);
				continue;
			}

			if (recursion) childList.AddRange(GetChildObjects<T>(child, recursion));
		}

		return childList;
	}
}