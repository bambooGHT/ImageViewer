using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageViewer.Services;
using ImageViewer.viewModels;
using Xceed.Wpf.Toolkit;

namespace ImageViewer.views;

public partial class SettingsView {
	private SettingsViewModel viewModel { get; }

	public SettingsView(SettingsManager settingsManager) {
		InitializeComponent();
		this.viewModel = new SettingsViewModel(settingsManager);
		this.DataContext = this.viewModel;
	}

	private void SaveBackground(object sender, EventArgs e) {
		var color = ((ColorPicker)sender).SelectedColorText;
		this.viewModel.UpdateCustomBackground(color);
	}

	private void InputKeyDown(object sender, KeyEventArgs e) {
		if (e.Key != Key.Enter) return;

		e.Handled = true;
		FocusManager.SetFocusedElement(this, this);
	}

	private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		if (int.TryParse(InputTextBox.Text, out int seconds)) {
			SecondsTextBlock.Text = $"{seconds / 1000.0} 秒";
		}
	}

	private void SaveSettings(object sender, RoutedEventArgs e) {
		this.viewModel.SaveSettings();
		this.Close();
	}

	protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
		if (Keyboard.FocusedElement is { IsMouseOver: false }) {
			FocusManager.SetFocusedElement(this, this);
		}

		base.OnPreviewMouseDown(e);
	}
}