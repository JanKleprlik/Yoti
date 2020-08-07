using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Yoti.Shared.Models
{
	public class Task : INotifyPropertyChanged
	{
		//TODO: implement INotifyPropertyChanged interface event invokes


		// Regular properties
		public string Name { get; set; }
		public short Difficulty { get; set; }
		public TimeSpan Time {get; set;}
		public DateTime Deadline { get; set; }
		public Tag Tag { get; set; }
		public string Description { get; set; }
		public List<Task> Dependencies = new List<Task>();
		public bool IsSeparable { get; set; }

		// Properties for automatically generated tasks
		public bool IsAutomatic { get; set; }
		public TimeSpan Period { get; set; }
		public string Email { get; set; }   // TODO: May be replaced with more sophisticated type
		public string DefaultEmailText { get; set; }


		// INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;


	}
}
