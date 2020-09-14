using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Yoti.Shared.Controls
{
	public sealed class CustomProgressBar : Control
	{
		public CustomProgressBar()
		{
			this.DefaultStyleKey = typeof(CustomProgressBar);
		}

		public double Progress
		{
			get { return (double) GetValue(ProgressProperty); }
			set { SetValue(ProgressProperty, value); }
		}

		public static readonly  DependencyProperty ProgressProperty =
			DependencyProperty.Register("Progress", typeof(double), typeof(CustomProgressBar), new PropertyMetadata(0d));
	}
}
