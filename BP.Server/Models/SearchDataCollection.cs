using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BP.Server.Models
{
	public class SearchDataCollection
	{
		private readonly ConcurrentDictionary<uint, ConcurrentBag<ulong>> _searchData
			= new ConcurrentDictionary<uint, ConcurrentBag<ulong>>();

		public ConcurrentDictionary<uint, ConcurrentBag<ulong>> SearchData => _searchData;

	}
}
