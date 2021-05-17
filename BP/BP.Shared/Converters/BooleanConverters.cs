using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace BP.Shared.Converters
{
	/// <summary>
	/// Converts Bool to Visibility <br></br>
	/// True - Visible <br></br>
	/// False - Collapsed
	/// </summary>
	class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is Boolean && (bool)value)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value is Visibility && (Visibility)value == Visibility.Visible)
			{
				return true;
			}
			return false;
		}
	}
	/// <summary>
	/// Converts Bool to Visibility <br></br>
	/// True - Collapsed <br></br>
	/// False - Visible
	/// </summary>
	class BooleanToVisibilityInvertedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is Boolean && (bool)value)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value is Visibility && (Visibility)value == Visibility.Visible)
			{
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// NOT operation over boolean
	/// </summary>
	class InverseBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (!(value is Boolean))
			{
				throw new ArgumentException($"Argument is not of type boolean.");
			}
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
