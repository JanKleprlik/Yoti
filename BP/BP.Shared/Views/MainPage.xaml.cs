using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;

using System.Threading;
using System.Text.RegularExpressions;
#if NETFX_CORE
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
#endif

#if __ANDROID__
using Android.Media;
using Android.Content.PM;
#endif

#if __WASM__
using Uno.Foundation;
#endif

namespace BP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

		Shared.AudioRecorder.Recorder recorder;
#region WASM

#endregion

		public MainPage()
        {
            this.InitializeComponent();
			recorder = new Shared.AudioRecorder.Recorder();
            textBlk.Text = "I am ready";
		}




		private async void recordBtn_Click(object sender, RoutedEventArgs e)
        {
			if (await recorder.StartRecording())
			{
				textBlk.Text = "Called library and am recording...";
			}
#region WASM
#if __WASM__

                MainPage.FileSelectedEvent -= OnFileSelectedEvent;
                MainPage.FileSelectedEvent += OnFileSelectedEvent;
                WebAssemblyRuntime.InvokeJS(@"
    console.log('calling javascript');
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.wav';
    input.onchange = e => {
        var file = e.target.files[0];
        if ((file.size /1024 /1024)>5){
            alert('File size exceeds 5 MB');
        }
        else
        {
            console.log(file.size);
            var reader = new FileReader();
            reader.readAsDataURL(file);
            reader.onload = readerEvent => {
                var content = readerEvent.target.result; // this is the content
                console.log('spitting out content');
                console.log(content);
                var selectFile = Module.mono_bind_static_method(" + "\"[BP.Wasm] BP.MainPage:SelectFile\""+@");
                selectFile(content);
        }
        };
    };
    input.click(); ");
#endif
#endregion
            
		}

		private async void stopBtn_Click(object sender, RoutedEventArgs e)
        {
			if (await recorder.StopRecording())
			{
				textBlk.Text = "Stopped recording from lib.";
			}
        }

        private async void playBtn_Click(object sender, RoutedEventArgs e)
        {
		#region UWP
#if NETFX_CORE
			if (await recorder.ReplayRecordingUWP(Dispatcher))
			{
				textBlk.Text = "Replaying recorded sound.";				
			}
#endif
		#endregion
		#region ANDROID
#if __ANDROID__
		if (await recorder.ReplayRecordingANDROID())
		{
			textBlk.Text = "Replaying recorded sound.";
		}
#endif
		#endregion
        }
	/*
        public static void SelectFile(string imageAsDataUrl) => FileSelectedEvent?.Invoke(null, new FileSelectedEventHandlerArgs(imageAsDataUrl));

        private void OnFileSelectedEvent(object sender, FileSelectedEventHandlerArgs e)
        {
            FileSelectedEvent -= OnFileSelectedEvent;
            var base64Data = Regex.Match(e.FileAsDataUrl, @"data:audio/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
            //Console.Out.WriteLine("[DEBUG] bas64Data:");
            //Console.Out.WriteLine("[DEBUG] len: " + base64Data.Length);
            var binData = Convert.FromBase64String(base64Data); //this is the data I want
			//Console.Out.WriteLine("[DEBUG] binData:");
            //Console.Out.WriteLine("[DEBUG] len: " + binData.Length);
            //print first 100 ascii chars
            for (int i = 0; i < 100; i++)
			{
			    Console.Out.WriteLine((char)binData[i]);
			}
		}

        private static event FileSelectedEventHandler FileSelectedEvent;

        private delegate void FileSelectedEventHandler(object sender, FileSelectedEventHandlerArgs args);

        private class FileSelectedEventHandlerArgs
        {
            public string FileAsDataUrl { get; }
            public FileSelectedEventHandlerArgs(string fileAsDataUrl) => FileAsDataUrl = fileAsDataUrl;

        }
	*/
    }
}
