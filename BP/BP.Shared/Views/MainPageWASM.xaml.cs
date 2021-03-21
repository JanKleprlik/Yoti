using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;


#if __WASM__
using Windows.UI.Xaml.Controls;
using System.Text.RegularExpressions;
using Uno.Foundation;
#endif


namespace BP.Shared.Views
{
	public sealed partial class MainPage : Page
	{
		#region WASM
#if __WASM__
		private async void uploadBtn_Click(object sender, RoutedEventArgs e)
		{
			FileSelectedEvent -= OnSongToRecognizeUploadedEvent;
			FileSelectedEvent += OnSongToRecognizeUploadedEvent;
			WebAssemblyRuntime.InvokeJS(@"
				console.log('calling javascript');
				var input = document.createElement('input');
				input.type = 'file';
				input.accept = '.wav';
				input.onchange = e => {
					var file = e.target.files[0];
					//size in MBs cannot be bigger than 5
					if ((file.size / 1024 / 1024)>5){ 
						alert('File size exceeds 5 MB');
					}
					else
					{
						var reader = new FileReader();
						reader.readAsDataURL(file);
						reader.onload = readerEvent => {
							//this is the binary uploaded content
							var content = readerEvent.target.result; 
							//invoke C# method to get audio binary data
							var selectFile = Module.mono_bind_static_method(" + "\"[BP.Wasm] BP.MainPage:SelectFile\"" + @");
							selectFile(content);
						}
					};
				};
				input.click(); "
			);
		}
		public static void SelectFile(string fileAsDataUrl) => FileSelectedEvent?.Invoke(null, new FileSelectedEventHandlerArgs(fileAsDataUrl));

		private void OnSongToRecognizeUploadedEvent(object sender, FileSelectedEventHandlerArgs e)
		{
			FileSelectedEvent -= OnSongToRecognizeUploadedEvent;
			var base64Data = Regex.Match(e.FileAsDataUrl, @"data:audio/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
			var binData = Convert.FromBase64String(base64Data); //this is the data I want
#if DEBUG
			for (int i = 0; i < 10; i++)
			{
				Console.Out.Write((char)binData[i]);
			}
			Console.Out.WriteLine();
#endif
			//recordedSong = binData;
			Console.Out.WriteLine("Recognize song");
		}

		private void OnNewSongUploadedEvent(object sender, FileSelectedEventHandlerArgs e)
		{
			FileSelectedEvent -= OnNewSongUploadedEvent;
			var base64Data = Regex.Match(e.FileAsDataUrl, @"data:audio/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
			uploadedSong = Convert.FromBase64String(base64Data); //this is the data I want
#if DEBUG
			for (int i = 0; i < 10; i++)
			{
				Console.Out.Write((char)uploadedSong[i]);
			}
			Console.Out.WriteLine();
#endif
			Console.Out.WriteLine("New song");
			//uploadedSong = binData;
		}

		private static event FileSelectedEventHandler FileSelectedEvent;

		private delegate void FileSelectedEventHandler(object sender, FileSelectedEventHandlerArgs args);

		private class FileSelectedEventHandlerArgs
		{
			public string FileAsDataUrl { get; }
			public FileSelectedEventHandlerArgs(string fileAsDataUrl) => FileAsDataUrl = fileAsDataUrl;

		}
#endif
		#endregion

	}
}
