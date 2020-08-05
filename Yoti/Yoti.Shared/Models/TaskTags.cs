using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Yoti.Shared.Models
{
	struct Tag
	{
		public Tag(string name, Color color)
		{
			Name = name;
			Color = color;
		}

		public string Name { get; }
		public Color Color { get; }

	}
}
