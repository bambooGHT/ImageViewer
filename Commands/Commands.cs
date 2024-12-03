using System.Windows;

namespace ImageViewer.Commands;

public static class Commands {
	public static RelayCommand CloseWindowCommand => new RelayCommand(CloseWindow);

	private static void CloseWindow(object? obj) {
		Application.Current.Windows[^1]!.Close();
	}
}