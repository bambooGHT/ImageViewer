using System.Configuration;
using System.Reflection;

namespace ImageViewer.utils;

public static class WpfAppConfig {
	/// <summary>
	/// 读取配置并赋值,如果配置选项不存在则跳过
	/// </summary>
	/// <param name="obj">要赋值的对象</param>
	/// <param name="propertyNames">属性名列表</param>
	public static void LoadConfigToObject(object obj, string[]? propertyNames = null) {
		Type objType = obj.GetType();
		propertyNames ??= GetObjectPropertyNames(objType);
		if (GetValue(propertyNames[0]) == null) return;

		foreach (string attr in propertyNames) {
			PropertyInfo? propertyInfo = objType.GetProperty(attr);
			if (propertyInfo == null) continue;

			Type propertyType = propertyInfo.PropertyType;
			Type actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

			var value = GetValue(attr, actualType);
			if (value != null) propertyInfo.SetValue(obj, value);
		}
	}

	public static void SaveObjectToConfig(object obj, string[]? propertyNames = null) {
		Type objType = obj.GetType();
		propertyNames ??= GetObjectPropertyNames(objType);
		Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
		KeyValueConfigurationCollection? settings = cfg.AppSettings.Settings;

		foreach (string attr in propertyNames) {
			PropertyInfo? propertyInfo = objType.GetProperty(attr);
			if (propertyInfo == null) continue;

			var value = propertyInfo.GetValue(obj)!;
			if (settings[attr] == null) {
				settings.Add(attr, value.ToString());
			} else {
				settings[attr].Value = value.ToString();
			}
		}

		WpfAppConfig.UpdateAppSettings(cfg);
	}

	public static object? GetValue(string key, Type type) {
		var value = GetValue(key);
		if (value == null) return null;
		return type.IsEnum ? Enum.Parse(type, value) : Convert.ChangeType(value, type);
	}

	public static string? GetValue(string key) {
		return ConfigurationManager.AppSettings[key];
	}

	public static void SetValue(string key, object value) {
		Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
		cfg.AppSettings.Settings[key].Value = value.ToString();
		WpfAppConfig.UpdateAppSettings(cfg);
	}

	private static void UpdateAppSettings(Configuration cfg) {
		cfg.Save(ConfigurationSaveMode.Modified);
		ConfigurationManager.RefreshSection("appSettings");
	}

	private static string[] GetObjectPropertyNames(Type objType) {
		return objType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(prop => prop.CanRead && prop.CanWrite && !Attribute.IsDefined(prop, typeof(DoNotSaveAttribute)))
			.Select(p => p.Name)
			.ToArray();
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class DoNotSaveAttribute : Attribute { }
}