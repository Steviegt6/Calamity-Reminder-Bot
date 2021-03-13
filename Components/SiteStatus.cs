﻿using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace tModloaderDiscordBot.Components
{
	public enum SiteStatusCode
	{
		Offline,
		Online,
		Unknown,
		Invalid
	}

	public class SiteStatus
	{
		[JsonIgnore] internal static IDictionary<SiteStatusCode, string> StatusCodes =
			new Dictionary<SiteStatusCode, string>
			{
				{ SiteStatusCode.Invalid, "Invalid address" },
				{ SiteStatusCode.Online, "Online (Response OK)" },
				{ SiteStatusCode.Offline, "Offline (Response OK)" }
			};

		public string Address;
		[JsonIgnore] public string CachedResult;

		public string Name;
		[JsonIgnore] public SiteStatusCode StatusCode = SiteStatusCode.Unknown;

		public static bool IsValidEntry(ref string addr)
		{
			CheckUriPrefix(ref addr);
			return IsUriLegit(addr, out Uri _);
		}

		public static bool IsUriLegit(string addr, out Uri uri) =>
			Uri.TryCreate(addr, UriKind.Absolute, out uri)
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

		public static void CheckUriPrefix(ref string addr)
		{
			if (addr.StartsWith("www."))
			{
				addr = addr.Substring(3);
			}

			if (!addr.StartsWith("http://") && !addr.StartsWith("https://"))
			{
				addr = $"http://{addr}";
			}
		}

		public void Revalidate()
		{
			bool result = IsValidEntry(ref Address);

			if (!result)
			{
				StatusCode = SiteStatusCode.Invalid;
			}

			try
			{
				StatusCode = (SiteStatusCode) Convert.ToInt32(Ping());
			}
			catch (Exception)
			{
				StatusCode = SiteStatusCode.Invalid;
			}

			CachedResult = StatusCodes[StatusCode];
		}

		internal bool Ping()
		{
			WebRequest request = WebRequest.Create(Address);
			return request.GetResponse() is HttpWebResponse response && response.StatusCode == HttpStatusCode.OK;
		}
	}
}