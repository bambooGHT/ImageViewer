using System.Linq.Expressions;
using System.Reflection;
using ImageViewer.Models;
using ImageViewer.utils;

namespace ImageViewer.Services;

public class SettingsManager {
	public Settings settings { get; } = LoadSettings();

	public void UpdateProperty<T>(Expression<Func<Settings, T>> propertyLambda, T newValue) {
		var propertyInfo = ((propertyLambda.Body as MemberExpression)?.Member as PropertyInfo)!;
		if (propertyInfo.CanWrite) {
			propertyInfo.SetValue(this.settings, newValue);
		}
	}

	public void UpdateSettings(Settings s) {
		ObjectUtils.MergeObjects(this.settings, s);
	}

	public void SaveSettings() {
		WpfAppConfig.SaveObjectToConfig(this.settings);
	}

	private static Settings LoadSettings() {
		var s = new Settings();
		WpfAppConfig.LoadConfigToObject(s);

		return s;
	}
}