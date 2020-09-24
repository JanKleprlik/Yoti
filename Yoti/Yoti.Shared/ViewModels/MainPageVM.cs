using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI;
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
			new Tag("All", Colors.Blue),
			new Tag("Free Time", Colors.Green),
			new Tag("Work", Colors.Purple),
			new Tag("School", Colors.Red),
			new Tag("Friends", Colors.Cyan),
			new Tag("Offroad", Colors.Aquamarine),
			new Tag("Alternative", Colors.Orange),
			new Tag("A", Colors.Tomato),
			new Tag("Zeta", Colors.Brown),

		};


		public MainPageVM()
		{
			Tasks.Add(new Task
			{
				Name = "Running",
				Difficulty = 2,
				Time = TimeSpan.FromHours(5),
				Deadline = DateTime.Now,
				Tag = Tags[1],
				Description = "This is very short description. 1",
				IsSeparable = false,
			});

			Tasks.Add(new Task
			{
				Name = "name of the task 2",
				Difficulty = 5,
				Time = TimeSpan.FromHours(2.5),
				Deadline = DateTime.Now,
				Tag = Tags[2],
				Description = "FREE",
				IsSeparable = false,

			});
			Tasks.Add(new Task
			{
				Name = "name of the task 3",
				Difficulty = 5,
				Time = TimeSpan.FromHours(3),
				Deadline = DateTime.Now,
				Tag = Tags[3],
				Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed porta, sem sed imperdiet vestibulum, metus ante sollicitudin lorem, in mattis nunc justo et sem. Phasellus porta ullamcorper nunc a ornare. Curabitur aliquet tempor ante sollicitudin porta. Nulla euismod nunc vitae orci faucibus pulvinar. Nam et ligula sapien. In pulvinar placerat porta. Duis facilisis placerat nunc, eget aliquet libero tincidunt sed. Aliquam erat volutpat.\r\n\r\nPhasellus bibendum dictum convallis. Vivamus laoreet vehicula tortor nec condimentum. Morbi tempus orci vitae nulla congue mollis. Aenean dignissim vehicula arcu id pretium. Curabitur nec vestibulum nisi. In vehicula cursus nunc sit amet rutrum. Aenean eget erat elementum mi pellentesque consequat. Sed in suscipit urna. Ut ac eros metus. Sed eu cursus tellus. Quisque a orci in nulla finibus sagittis at in lectus. Phasellus eu leo vel lacus pretium rhoncus vel in arcu. Vivamus ut nibh nibh. Quisque ut sodales leo. Nullam convallis quam in erat facilisis condimentum.",
				IsSeparable = false,

			});
			Tasks.Add(new Task
			{
				Name = "name of the task 4",
				Difficulty = 2,
				Time = TimeSpan.FromHours(2),
				Deadline = DateTime.Now,
				Tag = Tags[4],
				Description = "This is also very short description.",
				IsSeparable = false,

			});

		}

		public ObservableCollection<Task> Tasks = new ObservableCollection<Task>();

	}
}
