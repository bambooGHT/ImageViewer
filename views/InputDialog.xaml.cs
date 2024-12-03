using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageViewer.views;

public partial class InputDialog {
	public InputDialog(int value) {
		InitializeComponent();
		InputTextBox.Text = value.ToString();
		InputTextBox.Focus();
	}

	public int InputValue { get; private set; }

	private void OkButton_Click(object sender, RoutedEventArgs e) {
		InputValue = InputTextBox.Text == "" ? 100 : int.Parse(InputTextBox.Text);
		if (InputValue < 100) InputValue = 100;
		DialogResult = true;
		Close();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e) {
		DialogResult = false;
		Close();
	}

	private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e) {
		if (!int.TryParse(InputTextBox.Text, out int milliseconds)) return;

		InputTextBox.Text = milliseconds switch {
			> 60000 => "60000",
			_ => InputTextBox.Text
		};
		double seconds = int.Parse(InputTextBox.Text);
		SecondsTextBlock.Text = seconds < 100 ? "" : $"{seconds / 1000.0} 秒";
		InputTextBox.CaretIndex = InputTextBox.Text.Length;
	}

	private void InputKeyDown(object sender, KeyEventArgs e) {
		if (e.Key != Key.Enter) return;
		this.OkButton_Click(sender, e);
	}
}