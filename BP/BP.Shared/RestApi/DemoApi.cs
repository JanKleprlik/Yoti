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
				$"https://webapicore20210412194008.azurewebsites.net/values/333",
				new Dictionary<string, string> {
					{"accept", "application/x-www-form-urlencoded" },
				});


			if (result != null)
			{
				return result;
			}

			return "WRONG";
		}
	}
}
