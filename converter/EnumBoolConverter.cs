using System.Globalization;
using System.Windows.Data;

namespace ImageViewer.converter;

public class EnumBoolConverter: IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		return value.Equals(parameter);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotImplementedException();
	}
}