using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BP.Shared.ViewModels
{
	/// <summary>
	/// Base view model abstract class implementing OnPropertyChanged method.
	/// </summary>
	public abstract class BaseViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Notifies binded controls about change.
		/// </summary>
		/// <param name="propertyName">Name of changed property. This is optinal and can be provided automatically <see cref="CallerMemberNameAttribute"/>.</param>
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
