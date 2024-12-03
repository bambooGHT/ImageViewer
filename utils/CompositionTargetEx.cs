using System.Windows.Media;

namespace ImageViewer.utils;

public static class CompositionTargetEx {
	private static TimeSpan _last = TimeSpan.Zero;
	private static event EventHandler<RenderingEventArgs> _FrameUpdating = null!;
	public static event EventHandler<RenderingEventArgs> FrameUpdating {
		add {
			if (_FrameUpdating == null) CompositionTarget.Rendering += CompositionTarget_Rendering;
			_FrameUpdating += value;
		}
		remove {
			_last = new(0);
			_FrameUpdating -= value;
			if (_FrameUpdating == null) CompositionTarget.Rendering -= CompositionTarget_Rendering;
		}
	}

	private static void CompositionTarget_Rendering(object? sender, EventArgs e) {
		var args = e as RenderingEventArgs;
		if (args!.RenderingTime == _last) return;
		_last = args.RenderingTime;
		_FrameUpdating(sender, args);
	}
}