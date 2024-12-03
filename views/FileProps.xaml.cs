using System.IO;
using System.Windows;
using System.Windows.Input;
using ImageViewer.Models;

namespace ImageViewer.views;

public partial class FileProps {
	public ImageInfo imageInfo { get; }
	public FileInfo fileInfo { get; }
	public FileAttributes attributes { get; }
	public string formatFilePath { get; }

	public FileProps(ImageInfo imageInfo) {
		InitializeComponent();
		this.imageInfo = imageInfo;
		this.fileInfo = new FileInfo(imageInfo.filePath);
		this.attributes = File.GetAttributes(imageInfo.filePath);
		this.formatFilePath =
			fileInfo.DirectoryName!.Length > 25 ? $"{fileInfo.DirectoryName!.AsSpan(0, 25)}..." : fileInfo.DirectoryName!;
		this.DataContext = this;
	}

	private void OnClick(object sender, RoutedEventArgs e) {
		this.Close();
	}
}