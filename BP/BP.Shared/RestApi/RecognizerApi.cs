using System.Collections.Generic;
using System.Threading.Tasks;
using Database;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BP.Shared.RestApi
{
	public class RecognizerApi : WebApiBase
	{
		private Dictionary<string, string> defaultHeaders = new Dictionary<string, string> {
				{"accept", "application/json" },
			};
		private readonly string baseUrl = "https://yotiserver.azurewebsites.net/recognition";

		public async Task<Song> UploadSong(SongWavFormat songToUpload)
		{
			var result = await PostAsync(
				baseUrl + "/addnewsong",
				JsonSerializer.Serialize(songToUpload),
				defaultHeaders);

			this.Log().LogDebug(result);

			if (result != null)
			{
				return JsonSerializer.Deserialize<Song>(result);
			}

			return null;
		}
	
		public async Task<RecognitionResult> RecognizeSong(SongWavFormat songToRecognize)
		{
			var result = await PostAsync(
				baseUrl + "/recognizesong",
				JsonSerializer.Serialize(songToRecognize),
				defaultHeaders);

			this.Log().LogDebug(JsonSerializer.Serialize(songToRecognize));
			this.Log().LogDebug(result);

			if (result != null)
			{
				return JsonSerializer.Deserialize<RecognitionResult>(result);
			}

			return null;
		}

		public async Task<List<Song>> GetSongs()
		{
			var result = await GetAsync(
			   baseUrl + "/getsongs",
			   defaultHeaders);

			this.Log().LogDebug(result);

			if (result != null)
			{
				return JsonSerializer.Deserialize<List<Song>>(result);
			}

			return new List<Song>();
		}
		
		public async Task<Song> DeleteSong(Song songToDelete)
		{
			var result = await this.DeleteAsync(
				baseUrl + "/deletesong",
				JsonSerializer.Serialize(songToDelete),
				defaultHeaders);

			if (result != null)
			{
				return JsonSerializer.Deserialize<Song>(result);
			}

			return null;
		}
		
	
	}
}
