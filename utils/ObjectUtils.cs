using System.Reflection;

namespace ImageViewer.utils;

public static class ObjectUtils {
	public static T Clone<T>(T obj) where T : new() {
		var result = new T();
		return ObjectUtils.MergeObjects(result, obj);
	}

	public static T MergeObjects<T>(T target, T source) {
		Type t = typeof(T);

		var properties = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(prop => prop.CanRead && prop.CanWrite);

		foreach (PropertyInfo prop in properties) {
			var sourceValue = prop.GetValue(source);
			var targetValue = prop.GetValue(target);
			if (sourceValue != null && !sourceValue.Equals(targetValue)) prop.SetValue(target, sourceValue, null);
		}

		return target;
	}
}