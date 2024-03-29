﻿using Google.Apis.Customsearch.v1;
using Google.Apis.Customsearch.v1.Data;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using Imgur.API.Models.Impl;
using Skuld.Core.Extensions;
using Skuld.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;

namespace Skuld.APIS
{
	public static class SearchClient
	{
		private static CustomsearchService GoogleSearchService;
		private static YoutubeClient Youtube;
		private static ImgurClient ImgurClient;
		private static string GoogleCxKey;

		public static void Configure(string GoogleAPIKey, string GoogleCx, string imgurClientID, string imgurClientSecret)
		{
			GoogleCxKey = GoogleCx;
			GoogleSearchService = new CustomsearchService();

			Youtube = new YoutubeClient();

			if (!string.IsNullOrEmpty(imgurClientID) && !string.IsNullOrEmpty(imgurClientSecret))
			{
				ImgurClient = new ImgurClient(imgurClientID, imgurClientSecret);
			}

			if (!string.IsNullOrEmpty(GoogleAPIKey))
			{
				GoogleSearchService = new CustomsearchService(new Google.Apis.Services.BaseClientService.Initializer { ApiKey = GoogleAPIKey, ApplicationName = "Skuld" });
			}
		}

		public static async Task<string> SearchImgurAsync(string query)
		{
			if (ImgurClient == null) return null;
			try
			{
				var endpoint = new GalleryEndpoint(ImgurClient);
				var images = await endpoint.SearchGalleryAsync(query).ConfigureAwait(false);
				var albm = images.RandomValue();
				dynamic album = null;
				if (albm is GalleryImage)
				{
					album = albm as IGalleryImage;
				}
				if (albm is GalleryAlbum)
				{
					album = albm as IGalleryAlbum;
				}
				if (album != null && !album.Nsfw)
				{
					return "I found this:\n" + album.Link;
				}
				else
				{
					return "I found nothing sorry. :/";
				}
			}
			catch (Exception ex)
			{
				return $"Error with search: {ex.Message}";
			}
		}

		public static async Task<string> SearchImgurNSFWAsync(string query)
		{
			if (ImgurClient == null) return null;
			try
			{
				var endpoint = new GalleryEndpoint(ImgurClient);
				var images = await endpoint.SearchGalleryAsync(query).ConfigureAwait(false);
				var albm = images.RandomValue();
				dynamic album = null;
				if (albm is GalleryImage)
				{
					album = albm as IGalleryImage;
				}
				if (albm is GalleryAlbum)
				{
					album = albm as IGalleryAlbum;
				}
				if (album != null && !album.Nsfw)
				{
					return "I found this:\n" + album.Link;
				}
				else
				{
					return "I found nothing sorry. :/";
				}
			}
			catch
			{
				return null;
			}
		}

		public static async Task<string> SearchYoutubeAsync(string query)
		{
			try
			{
				var items = await Youtube.Search.GetVideosAsync(query);
				var item = items.FirstOrDefault();
				var totalreactions = item.Engagement.LikeCount + item.Engagement.DislikeCount;
				double ratiog = ((double)item.Engagement.LikeCount / totalreactions) * 100;
				double ratiob = ((double)item.Engagement.DislikeCount / totalreactions) * 100;

				return $"<:youtube:314349922885566475> | http://youtu.be/{item.Id}\n" +
					$"`👀: {item.Engagement.ViewCount.ToFormattedString()}`\n" +
					$"`👍: {item.Engagement.LikeCount.ToFormattedString()} ({ratiog:0.0}%)\t👎: {item.Engagement.DislikeCount:N0} ({ratiob:0.0}%)`\n" +
					$"`Duration: {item.Duration}`";
			}
			catch (Exception ex)
			{
				return $"Error with search: {ex.Message}";
			}
		}

		public static async Task<IReadOnlyCollection<Result>> SearchGoogleAsync(string query)
		{
			if (GoogleSearchService == null) return null;
			try
			{
				var listRequest = GoogleSearchService.Cse.List();
				listRequest.Q = query;
				listRequest.Cx = GoogleCxKey;
				listRequest.Safe = CseResource.ListRequest.SafeEnum.High;
				var search = await listRequest.ExecuteAsync().ConfigureAwait(false);
				var items = search.Items;
				if (items != null)
				{
					var item = items.FirstOrDefault();
					var item2 = items.ElementAtOrDefault(1);
					var item3 = items.ElementAtOrDefault(2);

					return new List<Result>
					{
						item,
						item2,
						item3
					}.AsReadOnly();
				}
				else
				{
					return null;
				}
			}
			catch (Exception ex)
			{
				Log.Error("Google", ex.Message, null, ex);
				return null;
			}
		}
	}
}