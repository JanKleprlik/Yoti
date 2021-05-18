using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yoti.Shared.Utils
{
	/// <summary>
	/// Class for getting permisions in runtime.
	/// </summary>
	static public class Permissions
	{
#if __ANDROID__
		static public class Droid
		{
			/// <summary>
			/// Obtains external storage permission.
			/// </summary>
			/// <returns>True if permission is granted, false otherwise.</returns>
			public static async Task<bool> GetExternalStoragePermission()
			{
				CancellationTokenSource source = new CancellationTokenSource();
				CancellationToken token = source.Token;
				return await Windows.Extensions.PermissionsHelper.TryGetPermission(token, Android.Manifest.Permission.ReadExternalStorage);
			}

			/// <summary>
			/// Obtains microphone permission.
			/// </summary>
			/// <returns>True if permission is granted, false otherwise.</returns>
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
