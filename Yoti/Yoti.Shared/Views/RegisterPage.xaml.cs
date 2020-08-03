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
	public sealed partial class RegisterPage : Page
	{
		public RegisterPage()
		{
			this.InitializeComponent();
			DataContext = new RegisterPageVM();
			NavigationCacheMode = NavigationCacheMode.Enabled;
		}


		private void GoBack(object sender, RoutedEventArgs e)
		{
			this.Frame.GoBack();
		}


		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.Parameter is string && !string.IsNullOrWhiteSpace((string)e.Parameter))
			{
				username.Text = e.Parameter.ToString();
				password1.Focus(FocusState.Keyboard);
			}


			base.OnNavigatedTo(e);
		}
	}
}
