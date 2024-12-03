using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageViewer.customControl;

public class NumberBox : TextBox {
	public Regex re = new("[^0-9]+");

	public NumberBox() {
		this.PreviewTextInput += LimitNumTextInput;
		DataObject.AddPastingHandler(this, OnPaste);
		InputMethod.SetIsInputMethodEnabled(this, false);
	}

	private void LimitNumTextInput(object sender, TextCompositionEventArgs e) {
		e.Handled = re.IsMatch(e.Text);
	}

	private void OnPaste(object sender, DataObjectPastingEventArgs e) {
		var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
		if (!isText) return;

		var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
		if (!int.TryParse(text, out _)) e.CancelCommand();
	}
}