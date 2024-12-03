using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageViewer.viewModels;

public class ViewModelBase : INotifyPropertyChanged {
	public event PropertyChangedEventHandler? PropertyChanged;
	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}
}