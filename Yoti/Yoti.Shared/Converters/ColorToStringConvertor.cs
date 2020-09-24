using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace Yoti.Shared.Converters
{
	public class ColorToStringConvertor : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (!(value is Color)) return value;
			return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
