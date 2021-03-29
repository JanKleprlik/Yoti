using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace BP.Shared.Converters
{
	class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			System.Diagnostics.Debug.WriteLine("IN HERE");
			System.Diagnostics.Debug.WriteLine((bool)value);
			if (value is Boolean && (bool)value)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			System.Diagnostics.Debug.WriteLine("IN HERE2");
			if (value is Visibility && (Visibility)value == Visibility.Visible)
			{
				return true;
			}
			return false;
		}
	}

}
