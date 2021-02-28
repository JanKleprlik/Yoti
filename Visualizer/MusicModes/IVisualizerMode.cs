using SFML.Graphics;

namespace Visualizer.MusicModes
{
	interface IVisualiserMode
	{
		void Draw(RenderWindow window);
		void Update();
		void Quit();
	}
}
