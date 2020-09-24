using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
	public sealed partial class TaskOverview : UserControl
	{
		public TaskOverview()
		{
			this.InitializeComponent();
			this.DefaultStyleKey = typeof(TaskOverview);
		}

		#region  Progress

		public double Progress
		{
			get => (double)GetValue(ProgressProperty);
			set => SetValue(ProgressProperty, value);
		}

		public static readonly DependencyProperty ProgressProperty =
			DependencyProperty.Register(
				"Progress",
				typeof(double),
				typeof(TaskOverview),
				new PropertyMetadata(0d, new PropertyChangedCallback(OnProgressChanged)));

		private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskOverview taskProgressBarControl = d as TaskOverview;
			taskProgressBarControl.OnProgressChanged(e);
		}

		private void OnProgressChanged(DependencyPropertyChangedEventArgs e)
		{
			this.ProgressBar.Progress = (double)e.NewValue;
		}

		#endregion

		#region TotalTime

		public double TotalTime
		{
			get => (double)GetValue(TotalTimeProperty);
			set => SetValue(TotalTimeProperty, value);
		}

		public static readonly DependencyProperty TotalTimeProperty =
			DependencyProperty.Register(
				"TotalTime",
				typeof(double),
				typeof(TaskOverview),
				new PropertyMetadata(0d, new PropertyChangedCallback(OnTotalTimeChanged)));

		private static void OnTotalTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskOverview taskTotalTime = d as TaskOverview;
			taskTotalTime.OnTotalTimeChanged(e);
		}

		private void OnTotalTimeChanged(DependencyPropertyChangedEventArgs e)
		{
			Total.Text = e.NewValue.ToString();
		}

		#endregion

		#region CompleteTime

		public double CompleteTime
		{
			get => (double)GetValue(CompleteTimeProperty);
			set => SetValue(CompleteTimeProperty, value);
		}

		public static readonly DependencyProperty CompleteTimeProperty =
			DependencyProperty.Register(
				"CompleteTime",
				typeof(double),
				typeof(TaskOverview),
				new PropertyMetadata(0d, new PropertyChangedCallback(OnCompleteTimeChanged)));

		private static void OnCompleteTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskOverview taskCompleteTime = d as TaskOverview;
			taskCompleteTime.OnCompleteTimeChanged(e);
		}

		private void OnCompleteTimeChanged(DependencyPropertyChangedEventArgs e)
		{
			Complete.Text = e.NewValue.ToString();
		}

		#endregion

		#region Units

		public string Units
		{
			get => (string)GetValue(UnitsProperty);
			set => SetValue(UnitsProperty, value);
		}

		public static readonly DependencyProperty UnitsProperty =
			DependencyProperty.Register(
				"Units",
				typeof(string),
				typeof(TaskOverview),
				new PropertyMetadata("", new PropertyChangedCallback(OnUnitsChanged)));
		private static void OnUnitsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskOverview unitsText = d as TaskOverview;
			unitsText.OnUnitsChanged(e);
		}

		private void OnUnitsChanged(DependencyPropertyChangedEventArgs e)
		{
			UnitsText.Text = (string)e.NewValue;
		}

		#endregion

		#region TagColor

		public Color TagColor
		{
			get => (Color)GetValue(TagColorProperty);
			set => SetValue(TagColorProperty, value);
		}

		public static readonly DependencyProperty TagColorProperty =
			DependencyProperty.Register(
				"TagColor",
				typeof(Color),
				typeof(TaskOverview),
				new PropertyMetadata("", new PropertyChangedCallback(OnTagColorChanged)));
		private static void OnTagColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskOverview unitsText = d as TaskOverview;
			unitsText.OnTagColorChanged(e);
		}

		private void OnTagColorChanged(DependencyPropertyChangedEventArgs e)
		{
			var clr = (Color) e.NewValue;
			TagBorder.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(clr.A, clr.R, clr.G, clr.B));
		}

		#endregion

		#region TagName

		public string TagName
		{
			get => (string)GetValue(TagProperty);
			set => SetValue(TagProperty, value);
		}

		public static readonly DependencyProperty TagProperty =
			DependencyProperty.Register(
				"TagName",
				typeof(string),
				typeof(TaskOverview),
				new PropertyMetadata("", new PropertyChangedCallback(OnTagChanged)));
		private static void OnTagChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskOverview o = d as TaskOverview;
			o.OnTagChanged(e);
		}

		private void OnTagChanged(DependencyPropertyChangedEventArgs e)
		{
			TagText.Text = (string)e.NewValue;
		}

		#endregion

		#region Description

		public string Description
		{
			get => (string)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static readonly DependencyProperty DescriptionProperty =
			DependencyProperty.Register(
				"Description",
				typeof(string),
				typeof(TaskOverview),
				new PropertyMetadata("", new PropertyChangedCallback(OnDescriptionChanged)));
		private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TaskOverview descriptionText= d as TaskOverview;
			descriptionText.OnDescriptionChanged(e);
		}

		private void OnDescriptionChanged(DependencyPropertyChangedEventArgs e)
		{
			DescriptionText.Text = (string)e.NewValue;
		}

		#endregion

	}
}
