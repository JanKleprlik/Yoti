using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BP.Shared.RestApi
{
	public class DemoApi : WebApiBase
	{
		public async Task<string> Search()
		{
			var result = await GetAsync(
				$"https://yotiserver.azurewebsites.net/weatherforecast",
				new Dictionary<string, string> {
					{"accept", "application/json" },
				});


			if (result != null)
			{
				return result;
			}

			return "WRONG";
		}
	}
}
