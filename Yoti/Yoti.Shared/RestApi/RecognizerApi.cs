using System.Collections.Generic;
using System.Threading.Tasks;
using SharedTypes;
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
		private readonly string baseUrl = "https://yotiserverdev.azurewebsites.net";
		#endregion

		/// <summary>
		/// Upload new song to the database.
		/// </summary>
		/// <param name="songToUpload">Preprocessed song data to be uploaded.</param>
		/// <returns>Uploaded song. Null on failure.</returns>
		public async Task<Song> UploadSong(PreprocessedSongData songToUpload)
		{
			string result;

			try
			{

				result = await PostAsync(
					baseUrl + "/recognition/addnewsong",
					JsonSerializer.Serialize(songToUpload, serializerOptions),
					defaultHeaders);
			}
			catch (System.Net.Http.HttpRequestException e)
			{
				//result is null when there is no connection
				result = null;
			}



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
		/// <returns>Recognition result containting info about recognized song and process of recognizing. Song null and empty SongAccuracies on failure.</returns>
		public async Task<RecognitionResult> RecognizeSong(PreprocessedSongData songToRecognize)
		{
			string result;
			try
			{
				result = await PostAsync(
					baseUrl + "/recognition/recognizesong",
					JsonSerializer.Serialize(songToRecognize, serializerOptions),
					defaultHeaders);
			}
			catch (System.Net.Http.HttpRequestException e)
			{
				//result is null when there is no connection
				result = null;
			}

			if (result != null)
			{
				return JsonSerializer.Deserialize<RecognitionResult>(result, serializerOptions);
			}

			return new RecognitionResult { Song = null, SongAccuracies = new List<System.Tuple<uint, double>>() };
		}

		/// <summary>
		/// Get list of songs in the database.
		/// </summary>
		/// <returns>List of songs in the database.</returns>
		public async Task<List<Song>> GetSongs()
		{
			string result;
			try
			{

				result = await GetAsync(
				   baseUrl + "/recognition/getsongs",
				   defaultHeaders);
			}
			catch(System.Net.Http.HttpRequestException e)
			{
				//result is null when there is no connection
				result = null;
			}

			if (result != null)
			{
				return JsonSerializer.Deserialize<List<Song>>(result, serializerOptions);
			}

			return new List<Song>();
		}
		
		/// <summary>
		/// Get song by its id.
		/// </summary>
		/// <param name="id">Id of the song</param>
		/// <returns>Song if it is in database. Null otherwise</returns>
		public async Task<Song> GetSongById(uint id)
		{
			string result;
			try
			{		
				result = await GetAsync(
					baseUrl + $"/recognition/getsong/{id}",
					defaultHeaders);
			}
			catch (System.Net.Http.HttpRequestException e)
			{
				//result is null when there is no connection
				result = null;
			}

			if (result != null)
			{
				return JsonSerializer.Deserialize<Song>(result, serializerOptions);
			}
			return null;
		}

		/// <summary>
		/// Delete song from database.
		/// </summary>
		/// <param name="songToDelete">Song to delete</param>
		/// <returns>Deleted song on sucess, null on failure.</returns>
		public async Task<Song> DeleteSong(Song songToDelete)
		{
			string result;
			try
			{
				result = await this.DeleteAsync(
					baseUrl + "/recognition/deletesong",
					JsonSerializer.Serialize(songToDelete, serializerOptions),
					defaultHeaders);
			}
			catch (System.Net.Http.HttpRequestException e)
			{
				//result is null when there is no connection
				result = null;
			}


			if (result != null)
			{
				return JsonSerializer.Deserialize<Song>(result, serializerOptions);
			}

			return null;
		}
		
	}
}
