using System.Configuration;
using System.Data;
using System.Windows;
using ImageViewer.Services;

namespace ImageViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
	public static SettingsManager settingsManager { get; } = new SettingsManager();
}