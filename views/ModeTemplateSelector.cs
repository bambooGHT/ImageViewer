using System.Windows;
using System.Windows.Controls;

namespace ImageViewer.views;

public class ModeTemplateSelector<T>(Dictionary<T, DataTemplate> templates) : DataTemplateSelector
	where T : notnull {
	public override DataTemplate? SelectTemplate(object? item, DependencyObject container) {
		return item == null ? null : templates.GetValueOrDefault((T)item);
	}
}