namespace ImageViewer.utils;

public class Debounce {
	private Timer? timer;
	private Action action = null!;
	private TimeSpan interval;

	private void DebounceAction() {
		timer?.Dispose();
		timer = new(state => {
			action();

			timer?.Dispose();
			timer = null;
		}, null, interval, Timeout.InfiniteTimeSpan);
	}

	public Action Create(Action func, TimeSpan timeout) {
		this.action = func;
		this.interval = timeout;
		return DebounceAction;
	}
}