using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions.Specialized;

namespace BP.Shared.RestApi
{
	public abstract class WebApiBase
	{
		protected static HttpClient _client;

		static WebApiBase()
		{
#if __WASM__
            var innerHandler = new Uno.UI.Wasm.WasmHttpHandler();
#else
			var innerHandler = new HttpClientHandler();
#endif
			_client = new HttpClient(innerHandler);
		}

		protected HttpRequestMessage CreateRequestMessage(HttpMethod method, string url, Dictionary<string, string> headers = null)
		{
			var httpRequestMessage = new HttpRequestMessage(method, url);
			if (headers != null && headers.Any())
			{
				foreach (var header in headers)
				{
					httpRequestMessage.Headers.Add(header.Key, header.Value);
				}
			}

			return httpRequestMessage;
		}

		protected async Task<string> GetAsync(string url, Dictionary<string, string> headers = null)
		{
			using (var request = CreateRequestMessage(HttpMethod.Get, url, headers))
			using (var response = await _client.SendAsync(request))
			{
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadAsStringAsync();
				}

				return null;
			}
		}
		protected async Task<string> DeleteAsync(string url, Dictionary<string, string> headers = null)
		{
			using (var request = CreateRequestMessage(HttpMethod.Delete, url, headers))
			using (var response = await _client.SendAsync(request))
			{
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadAsStringAsync();
				}

				return null;
			}
		}

		protected async Task<string> PostAsync(string url, string payload, Dictionary<string, string> headers = null)
		{
			using (var request = CreateRequestMessage(HttpMethod.Post, url, headers))
			{
				request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
				using (var response = await _client.SendAsync(request))
				{
					if (response.IsSuccessStatusCode)
					{
						return await response.Content.ReadAsStringAsync();
					}

					return null;
				}
			}
		}

		protected async Task<string> PutAsync(string url, string payload, Dictionary<string, string> headers = null)
		{
			using (var request = CreateRequestMessage(HttpMethod.Put, url, headers))
			{
				request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
				using (var response = await _client.SendAsync(request))
				{
					if (response.IsSuccessStatusCode)
					{
						return await response.Content.ReadAsStringAsync();
					}

					return null;
				}
			}
		}

	}
}
