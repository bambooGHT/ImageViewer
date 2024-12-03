using System.Globalization;
using System.Windows.Data;

namespace ImageViewer.converter;

public class EnumCompareConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value != null && parameter != null) {
			return !value.Equals(parameter);
		}

		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotImplementedException();
	}
}