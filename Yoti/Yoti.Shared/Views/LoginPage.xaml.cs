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
	public sealed partial class LoginPage : Page
	{
		public LoginPage()
		{
			this.InitializeComponent();
			DataContext = new LoginPageVM();
			NavigationCacheMode = NavigationCacheMode.Enabled;
		}

		private void OnRegister(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(RegisterPage), username.Text);
		}

		private void OnLogin(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(MainPage));
		}

	}
}
