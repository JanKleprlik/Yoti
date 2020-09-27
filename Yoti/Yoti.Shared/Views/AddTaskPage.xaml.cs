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
using Yoti.Shared.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Yoti.Shared.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class AddTaskPage : Page
	{
		public AddTaskPage()
		{
			this.InitializeComponent();
			MainPageVM MPVM = new MainPageVM();
			TagPicker.ItemsSource = MPVM.Tags;
		}

		public void OnCancel(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(MainPage));
		}
	}
}
