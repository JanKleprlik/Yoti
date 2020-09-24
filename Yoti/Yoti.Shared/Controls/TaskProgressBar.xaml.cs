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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Yoti.Shared.Controls
{
	public sealed partial class TaskProgressBar : UserControl
	{
		public TaskProgressBar()
		{
			this.InitializeComponent();
			this.DefaultStyleKey = typeof(TaskProgressBar); //needed for cross platform 
		}

		public double Progress
		{
			get => (double)GetValue(ProgressProperty);
			set => SetValue(ProgressProperty, value);
		}

		public static readonly DependencyProperty ProgressProperty =
			DependencyProperty.Register(
				"Progress",
				typeof(double),
				typeof(TaskProgressBar),
				new PropertyMetadata(10d, new PropertyChangedCallback(OnProgressChanged)));

		private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskProgressBar taskProgressBarControl = d as TaskProgressBar;
			taskProgressBarControl.OnProgressChanged(e);
		}

		private void OnProgressChanged(DependencyPropertyChangedEventArgs e)
		{
			Bar.Height = (double)e.NewValue;
		}
	}
}
