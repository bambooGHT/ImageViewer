using System.ComponentModel;

namespace ImageViewer.Models;

public static class Enums {
	public enum EViewMode {
		[Description("翻页模式")]
		SingleImage,
		[Description("滚动模式")]
		VerticalScroll
	}

	public enum EScaleType {
		[Description("完整大小")]
		FullSize,
		[Description("适合图像")]
		FitImage,
		[Description("适合宽度")]
		FitWidth,
		[Description("适合高度")]
		FitHeight,
		[Description("百分比")]
		Percentage
	}
}