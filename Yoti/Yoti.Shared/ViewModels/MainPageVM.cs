using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;
using System.Windows.Input;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using Yoti.Shared.Models;

namespace Yoti.Shared.ViewModels
{
	[Windows.UI.Xaml.Data.Bindable]
	public class MainPageVM 
	{

		public List<Tag> Tags = new List<Tag>
		{
			new Tag("All", Color.FromName("Blue")),
			new Tag("All", Color.FromName("Green")),
			new Tag("All", Color.FromName("Purple")),
			new Tag("All", Color.FromName("Red")),
			new Tag("All", Color.FromName("Cayan")),
			new Tag("All", Color.FromName("Blue")),
			new Tag("All", Color.FromName("Orange")),
			new Tag("All", Color.FromName("Gray")),
			new Tag("All", Color.FromName("Brown")),

		};


		public MainPageVM()
		{
			Tasks.Add(new Task { Name = "name of the task 1" });
			Tasks.Add(new Task { Name = "name of the task 2" });
			Tasks.Add(new Task { Name = "name of the task 3" });
			Tasks.Add(new Task { Name = "name of the task 4" });

		}

		public ObservableCollection<Task> Tasks = new ObservableCollection<Task>();

	}
}
