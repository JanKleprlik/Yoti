using System.Collections.Generic;
using System.Threading.Tasks;
using Database;
using System.Text.Json;
using Uno.Extensions;
using Microsoft.Extensions.Logging;

namespace Yoti.Shared.RestApi
{
	public class RecognizerApi : WebApiBase
	{
		#region private fields
		/// <summary>
		/// Common request headers.
		/// </summary>
		private Dictionary<string, string> defaultHeaders = new Dictionary<string, string> {
				{"accept", "application/json" },
			};
		
		/// <summary>
		/// Base JSON serializer options
		/// </summary>
		private JsonSerializerOptions serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

		/// <summary>
		/// Base Recognizer URL
		/// </summary>
		private readonly string baseUrl = "https://yotiserver.azurewebsites.net/recognition";
		#endregion

		/// <summary>
		/// Upload new song to the database.
		/// </summary>
		/// <param name="songToUpload">Preprocessed song data to be uploaded.</param>
		/// <returns>Uploaded song. Null on failure.</returns>
		public async Task<Song> UploadSong(PreprocessedSongData songToUpload)
		{
			var result = await PostAsync(
				baseUrl + "/addnewsong",
				JsonSerializer.Serialize(songToUpload, serializerOptions),
				defaultHeaders);


			if (result != null)
			{
				return JsonSerializer.Deserialize<Song>(result, serializerOptions);
			}

			return null;
		}
	
		/// <summary>
		/// Recognize song in the database.
		/// </summary>
		/// <param name="songToRecognize">Preprocessed data of the song to be recognized.</param>
		/// <returns>Recognition result containting info about recognized song and process of recognizing. Null on failure.</returns>
		public async Task<RecognitionResult> RecognizeSong(PreprocessedSongData songToRecognize)
		{
			var result = await PostAsync(
				baseUrl + "/recognizesong",
				JsonSerializer.Serialize(songToRecognize, serializerOptions),
				defaultHeaders);

			if (result != null)
			{
				return JsonSerializer.Deserialize<RecognitionResult>(result, serializerOptions);
			}

			return null;
		}

		/// <summary>
		/// Get list of songs in the database.
		/// </summary>
		/// <returns>List of songs in the database.</returns>
		public async Task<List<Song>> GetSongs()
		{
			var result = await GetAsync(
			   baseUrl + "/getsongs",
			   defaultHeaders);

			if (result != null)
			{
				return JsonSerializer.Deserialize<List<Song>>(result, serializerOptions);
			}

			return new List<Song>();
		}
		
		/// <summary>
		/// Delete song from database.
		/// </summary>
		/// <param name="songToDelete">Song to delete</param>
		/// <returns>Deleted song on sucess, null on failure.</returns>
		public async Task<Song> DeleteSong(Song songToDelete)
		{
			var result = await this.DeleteAsync(
				baseUrl + "/deletesong",
				JsonSerializer.Serialize(songToDelete, serializerOptions),
				defaultHeaders);

			if (result != null)
			{
				return JsonSerializer.Deserialize<Song>(result, serializerOptions);
			}

			return null;
		}
		
	}
}
