using System.ComponentModel;
using System.Reflection;
using System.Windows.Media;
using ImageViewer.Models;
using ImageViewer.Services;
using ImageViewer.utils;

namespace ImageViewer.viewModels;

public class SettingsViewModel(SettingsManager settingsManager) {
	private SettingsManager settingsManager { get; } = settingsManager;
	public Settings tempSetting { get; } = ObjectUtils.MergeObjects(new Settings(), settingsManager.settings);

	public IEnumerable<KeyValuePair<Enums.EViewMode, string>> enumViewModes => GetEnumDescriptions<Enums.EViewMode>();
	public IEnumerable<KeyValuePair<Enums.EScaleType, string>> enumScaleTypes => GetEnumDescriptions<Enums.EScaleType>();

	public void UpdateCustomBackground(string value) {
		this.tempSetting.customBackgroundText = value;
	}

	public void SaveSettings() {
		this.settingsManager.UpdateSettings(tempSetting);
	}

	public static IEnumerable<KeyValuePair<T, string>> GetEnumDescriptions<T>() where T : Enum {
		Type type = typeof(T);
		return Enum.GetValues(type)
			.Cast<T>()
			.Select(x => new KeyValuePair<T, string>(x,
				type.GetField(x.ToString())!.GetCustomAttribute<DescriptionAttribute>()!.Description));
	}
}