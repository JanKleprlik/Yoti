using System;
using System.Collections.Generic;
using System.Drawing;
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
using Yoti.Shared.Models;
using Yoti.Shared.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Yoti.Shared.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();
			var MPVM = new MainPageVM();

			foreach(Tag tag in MPVM.Tags)
			{
				ToggleButton b = new ToggleButton();
				b.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(tag.Color.A, tag.Color.R, tag.Color.G, tag.Color.B));
				b.Content = tag.Name;
				TagButtonsPanel.Children.Add(b);
			}
		}

		public void OnBack(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(LoginPage));
		}
	}
}
