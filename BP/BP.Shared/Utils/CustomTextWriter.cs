using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace BP.Shared.Utils
{
	/// <summary>
	/// TextWriter that displays text into assigned TextBlock
	/// </summary>
	public class TextBlockTextWriter : TextWriter
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="outputTextControl">TextBlock control that will display text written into this TextWriter.</param>
		public TextBlockTextWriter(TextBlock outputTextControl)
		{
			this.outputTextControl = outputTextControl;
		}
		/// <summary>
		/// Assigned TextControl wich displays written text.
		/// </summary>
		private TextBlock outputTextControl;

		public override Task WriteLineAsync(string value)
		{
			value += Environment.NewLine;
			return outputTextControl.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { outputTextControl.Text += value; }).AsTask();
		}

		public override void WriteLine()
		{
			outputTextControl.Text += Environment.NewLine;
		}

		/// <summary>
		/// Clears the Text Control.
		/// </summary>
		public void Clear()
		{
			outputTextControl.Text = "";
		}

		public override Encoding Encoding => Encoding.Unicode;
	}
}
