using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace BP.Shared
{

	public class TextBlockTextWriter : TextWriter
	{
		private TextBlock outputTextControl;

		public TextBlockTextWriter(TextBlock outputTextControl)
		{
			this.outputTextControl = outputTextControl;
		}

		public override void Write(char value)
		{
			outputTextControl.Text += value;
		}

		public override void Write(string value)
		{
			outputTextControl.Text += value;
		}

		public override void WriteLine(char value)
		{
			outputTextControl.Text += value;
		}

		public override void WriteLine(string value)
		{
			outputTextControl.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { outputTextControl.Text += value; });
			//outputTextControl.Dispatcher.RunAsync(() => { outputTextControl.Text += value; });

			//Dispatcher.Run(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			//{
			//	displayInfoText($"Song ID: {ID}");
			//});
			//outputTextControl.Text += value;
		}

		public override Task WriteLineAsync(string value)
		{
			value += "\r\n";
			return outputTextControl.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { outputTextControl.Text += value; }).AsTask();
		}

		public override void WriteLine()
		{
			outputTextControl.Text += "\n";
		}

		public override Encoding Encoding => Encoding.Unicode;
	}
}
