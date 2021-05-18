using Yoti.Shared.ViewModels;
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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Yoti.Shared.Views
{
    public sealed partial class LyricsDialog : ContentDialog
    {

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mainPageVM">Reference to the main page view model</param>
		public LyricsDialog(MainPageViewModel mainPageVM)
        {
            this.InitializeComponent();
			LyricsViewModel = new LyricsViewModel(mainPageVM);
		}

		/// <summary>
		/// Main page view model
		/// </summary>
		public LyricsViewModel LyricsViewModel;
    }
}
