using SFML.Graphics;

namespace Visualizer.MusicModes
{
	/// <summary>
	/// Common interface to all Visualisation modes
	/// </summary>
	interface IVisualiserMode
	{
		void Draw(RenderWindow window);
		void Update();
		void Quit();
	}
}
