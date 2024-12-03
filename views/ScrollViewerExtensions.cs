using System.Windows;
using System.Windows.Controls;

namespace ImageViewer.views;

public static class ScrollViewerExtensions {
	public static readonly DependencyProperty ScrollToTopProperty =
		DependencyProperty.RegisterAttached(
			"ScrollToTop",
			typeof(bool),
			typeof(ScrollViewerExtensions),
			new PropertyMetadata(false, OnScrollToTopChanged));

	public static bool GetScrollToTop(DependencyObject obj) {
		return (bool)obj.GetValue(ScrollToTopProperty);
	}

	public static void SetScrollToTop(DependencyObject obj, bool value) {
		obj.SetValue(ScrollToTopProperty, value);
	}

	private static void OnScrollToTopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
		if (d is not ScrollViewer scrollViewer || !(bool)e.NewValue) return;
		scrollViewer.ScrollToVerticalOffset(0);
		SetScrollToTop(scrollViewer, false);
	}
}