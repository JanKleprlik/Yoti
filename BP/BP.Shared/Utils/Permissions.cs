using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BP.Shared.Utils
{
	static public class Permissions
	{
#if __ANDROID__
		static public class Droid
		{
			public static async Task<bool> GetExternalStoragePermission()
			{
				CancellationTokenSource source = new CancellationTokenSource();
				CancellationToken token = source.Token;
				return await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.ReadExternalStorage);
			}

			public static async Task<bool> GetMicPermission()
			{
				CancellationTokenSource source = new CancellationTokenSource();
				CancellationToken token = source.Token;
				return await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.RecordAudio);
			}
		}
#endif
	}
}
