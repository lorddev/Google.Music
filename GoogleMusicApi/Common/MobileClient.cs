﻿using GoogleMusicApi.Requests;
using GoogleMusicApi.Requests.Data;
using GoogleMusicApi.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using GoogleMusicApi.Authentication;
using GoogleMusicApi.Sessions;

namespace GoogleMusicApi.Common
{
    /// <summary>
    /// An Easy to use Google Play Music Client, that can do everything but upload music.
    ///
    /// </summary>
    public class MobileClient : Client<MobileSession>
    {
        /// <summary>
        /// The <see cref="StreamQuality"/> in which to request from google while using <seealso cref="GetStreamUrl"/>
        /// </summary>
        public StreamQuality StreamQuality { get; set; }

        /// <summary>
        /// Create a new <see cref="MobileClient"/>.
        /// </summary>
        public MobileClient()
        {
        }

        #region Privates

        private bool CheckSession()
        {
#if DEBUG
            if (Session == null)
                throw new InvalidOperationException("Session Not Set! Try logging in again.");
            if (Session.AuthorizationToken == null)
                throw new InvalidOperationException(
                    "Session does not contain an Authorization Token! Try logging in again.");
            return true;
#else
            if (Session?.AuthorizationToken != null)
                return true;
            return false;
#endif
        }

        private TReuqest MakeRequest<TReuqest>()
            where TReuqest : StructuredRequest, new()
        {
            return new TReuqest();
        }

        #endregion Privates

        #region Account

        /// <summary>
        /// Login to Google Play Music with the specified email and password.
        /// This will make two requests to the Google Authorization Server, and collect an Authorization Token to use for all requests.
        /// </summary>
        /// <param name="email">The Email / Username of the google account</param>
        /// <param name="password">The Password / App Specific password (https://security.google.com/settings/security/apppasswords)</param>
        public sealed override async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                Debug.WriteLine($"Attempting Login ({email})...");

                Session = new MobileSession(new UserDetails(email, password, GoogleAuth.GetPcMacAddress()));
                return await Session.LoginAsync();
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the specified Authorization Token is still valid, and if it is collect required information to login.
        /// </summary>
        /// <param name="email">The Email of the account</param>
        /// <param name="token">The Master Authorization Token, if left null will use the Authorization Token associated to this <see cref="MobileClient"/> (If specified in constructor)</param>
        /// <returns></returns>
        public async Task<bool> LoginWithToken(string email, string token = null)
        {
            Debug.WriteLine($"Attempting Login ({email})...");
            try
            {
                Session = new MobileSession(new UserDetails(email, "", GoogleAuth.GetPcMacAddress()));
                return await Session.LoginAsync(token);
            }
            catch (HttpRequestException)
            {
                return false;
            }


        }

        #endregion Account

        #region List Requests

        /// <summary>
        /// Gets the current Situations. Suggestions
        /// </summary>
        /// <param name="situationType">The situation types you wish to receive</param>
        /// <returns>
        /// Information like:
        ///  - "Its Friday Morning..." : "Play Music for..."
        ///     - "Todays Biggest Hits"
        ///         - "Today's Dance Smashes"
        ///         - "Today's Pop Charts"
        ///         - "..."
        ///     - "Waking Up Happy"
        ///         - "Star Guitars"
        ///             - "Air Guitar Heroes"
        ///             - "..."
        ///         - "..."
        /// All Data above will also have <see cref="ArtReference"/>'s for each station / situation
        /// </returns>
        //TODO (Low): Find out what situation types exist
        //TODO (Medium): Convert the int[] to a SituationType[]
        public async Task<ListListenNowSituationResponse> ListListenNowSituationsAsync(params int[] situationType)
        {
            if (!CheckSession())
                return null;
            if (situationType == null)
            {
                situationType = new[] { 1 };
            }
            var requestData = new ListListenNowSituationsRequest(Session)
            {
                RequestSignals = new RequestSignal(RequestSignal.GetTimeZoneOffsetSecs()),
                SituationType = situationType
            };

            var request = MakeRequest<ListListenNowSituations>();
            var data = await request.GetAsync(requestData);
            return data;
        }

        /// <summary>
        /// Gets a list of suggestions (Albums or Stations).
        /// </summary>
        /// <returns>
        /// Information like:
        /// - "Skin" : "Flume" : "Skin is a music album by Flume"
        ///     - Reason: "New Release because you like this artist"
        /// - "Tourist Radio"
        ///     - Reason: "Similar to Hayden James"
        /// Each Entry will have two images, one with an aspect ratio of 1 and another with aspect ratio of 2.
        /// Each Entry will contain either a RadioStation or an Album, never both.
        /// </returns>
        //TODO (Low): Change ListenNowItem Type to Enum
        public async Task<ListListenNowTracksResponse> ListListenNowTracksAsync()
        {
            if (!CheckSession())
                return null;

            var request = MakeRequest<ListListenNowTracks>();
            var data = await request.GetAsync(new GetRequest(Session));
            return data;
        }

        /// <summary>
        /// Gets a list of <see cref="Playlist"/>'s associated to the account
        /// </summary>
        /// <param name="numberOfResults">How many playlists you wish to receive</param>
        /// <returns>
        /// A DataSet of <see cref="Playlist"/>'s
        ///
        /// Future - TODO (Medium): Add Support for NextPageToken
        /// </returns>
        public async Task<ResultList<Playlist>> ListPlaylistsAsync(int numberOfResults = 50)
        {
            if (!CheckSession())
                return null;
            var request = MakeRequest<ListPlaylists>();
            var data = await request.GetAsync(new ResultListRequest(Session)
            {
                MaxResults = numberOfResults
            });
            return data;
        }

        /// <summary>
        /// Gets a list of promoted <see cref="Track"/>'s
        /// </summary>
        /// <param name="numberOfResults">How many playlists you wish to receive</param>
        /// <returns>
        /// A DataSet of <see cref="Playlist"/>'s
        /// Future - TODO (Low): Maybe Thumbs Up List?
        /// Future - TODO (Medium): Add Support for NextPageToken
        /// </returns>

        public async Task<ResultList<Track>> ListPromotedTracksAsync(int numberOfResults = 1000)
        {
            if (!CheckSession())
                return null;

            var request = MakeRequest<ListPromotedTracks>();
            var data = await request.GetAsync(new ResultListRequest(Session)
            {
                MaxResults = numberOfResults
            });
            return data;
        }

        /// <summary>
        /// Gets a list of <see cref="StationCategory"/>'s.
        /// This is the same as "Browse Stations" on the mobile interface
        /// </summary>
        /// <returns>
        /// Information like:
        ///   - "Root"
        ///     - "Genres"
        ///         - "..."
        ///     - "Activities"
        ///         - "..."
        ///     - "Moods"
        ///         -"..."
        /// </returns>
        public async Task<ListStationCategoriesResponse> ListStationCategoriesAsync()
        {
            if (!CheckSession())
                return null;
            var request = MakeRequest<ListStationCategories>();
            var data = await request.GetAsync(new GetRequest(Session));
            return data;
        }

        public async Task<ExploreTabsResponse> ExploreTabsAsync(int tab = 2, int numberOfItems = 50)
        {
            if (!CheckSession())
                return null;
            var request = MakeRequest<ExploreTabs>();
            var data = await request.GetAsync(new ExploreTabsRequest(Session)
            {
                Tabs = tab,
                NumberOfItems = numberOfItems,
            });
            return data;
        }

        #endregion List Requests

        #region Gets

        /// <summary>
        /// Get the Google Play Music configuration key / values
        /// </summary>
        /// <returns>Your current Google Play Music configuration</returns>
        public async Task<Config> GetConfigAsync()
        {
            if (!CheckSession())
                return null;
            var request = MakeRequest<GetConfig>();
            var data = await request.GetAsync(new GetRequest(Session));
            return data;
        }

        /// <summary>
        /// Get information about the <see cref="StationSeed"/>
        /// </summary>
        /// <param name="seed">A <see cref="StationSeed"/> that MUST contain CuratedStationId.</param>
        /// <returns>
        /// An <see cref="GetRadioStationAnnotationResponse"/> of data associated to the <see cref="StationSeed"/>
        /// This will include information such as:
        ///     - Art
        ///     - Title
        ///     - Related Groups
        ///         - Stations
        ///         - Albums
        ///         - Artists
        /// </returns>
        public async Task<GetRadioStationAnnotationResponse> GetRadioStationAnnotationAsync(StationSeed seed)
        {
            if (!CheckSession())
                return null;
            if (seed.CuratedStationId == null)
                return null;

            var request = MakeRequest<GetRadioStationAnnotation>();
            var data = await request.GetAsync(new GetRadioStationAnnotationRequest(Session, seed));
            return data;
        }

        /// <summary>
        /// Get the Stream <see cref="Uri"/> for the specified <see cref="Track"/>.
        ///
        /// Quality settings are from StreamQuality in <see cref="MobileClient"/>.
        /// </summary>
        /// <param name="track">The <see cref="Track"/> you wish to get the stream Uri for.</param>
        /// <returns>A <see cref="Uri"/> that locates to an MP3 Audio Stream</returns>
        public async Task<Uri> GetStreamUrlAsync(Track track)
        {
            if (!CheckSession())
                return null;
            var request = MakeRequest<GetStreamUrl>();
            var data = await request.GetAsync(new StreamUrlGetRequest(Session, track, StreamQuality));
            return data;
        }

        /// <summary>
        /// Gets information about a <see cref="Track"/> from a trackId.
        /// </summary>
        /// <param name="trackId">The track you wish to get information on</param>
        /// <returns></returns>
        public async Task<Track> GetTrackAsync(string trackId)
        {
            if (!CheckSession())
                return null;

            var request = MakeRequest<GetTrack>();
            var data = await request.GetAsync(new GetTrackRequest(Session, trackId));
            return data;
        }

        /// <summary>
        /// Gets information about a <see cref="Album"/> from a albumId.
        /// This can include tracks, and the description
        /// </summary>
        /// <param name="albumId">The album you wish to get information on</param>
        /// <param name="includeTracks">Whether or not to include a track list with the album</param>
        /// <param name="includeDescription">Whether or not to include the description with the album</param>
        /// <returns>
        /// An album with requested information included
        /// </returns>
        public async Task<Album> GetAlbumAsync(string albumId, bool includeTracks = true, bool includeDescription = true)
        {
            if (!CheckSession())
                return null;

            var request = MakeRequest<GetAlbum>();
            var data = await request.GetAsync(new GetAlbumRequest(Session, albumId)
            {
                IncludeTracks = includeTracks,
                IncludeDescription = includeDescription,
            });
            return data;
        }

        #endregion Gets

        #region Other

        /// <summary>
        /// Search for a <see cref="Track"/> / <see cref="Album"/> / <see cref="Artist"/> / <see cref="Station"/> / <see cref="Genre"/>
        /// </summary>
        /// <param name="query">The google styled search query</param>
        /// <returns>
        ///  A <see cref="SearchResponse"/> which contains a large amount of data including any amount of the following:
        /// <see cref="Track"/> / <see cref="Album"/> / <see cref="Artist"/> / <see cref="Station"/> / <see cref="Genre"/>
        ///  </returns>
        public async Task<SearchResponse> SearchAsync(string query)
        {
            if (!CheckSession())
                return null;
            var request = MakeRequest<ExecuteSearch>();
            var data = await request.GetAsync(new SearchGetRequest(Session, query));
            return data;
        }

        /// <summary>
        /// Sends a request to Create Or Get (<see cref="EditRadioStationRequestCreateOrGetMutation"/>) tracks from a <see cref="StationSeed"/>.
        ///
        /// This Is still very experimental, and not guaranteed to work.
        /// </summary>
        /// <param name="requestData">The mutations you wish to execute</param>
        /// <returns>
        /// Data associated (<see cref="EditRadioStationReponse"/>) to the mutations you executed.
        /// </returns>
        //TODO (Medium): Check What other Mutations are possible.
        public async Task<EditRadioStationReponse> EditRadioStationAsync(params EditRadioStationRequestMutation[] requestData)
        {
            if (!CheckSession())
                return null;

            var request = MakeRequest<EditRadioStation>();
            var data = await request.GetAsync(new EditRadioStationRequest(Session, requestData));
            return data;
        }

        public async Task<RecordRealTimeResponse> SetTrackRatingAsync(Track track, Rating.RatingEnum rating)
        {
            if (!CheckSession())
                return null;
            var requestData = new RecordRealTimeRequest(Session)
            {
                CurrentTimeMillis = (DateTime.UtcNow - DateTime.Now).TotalMilliseconds.ToString("#"),
                Events = new[]
                {
                    new Event
                    {
                        CreatedTimestampMillis = (DateTime.UtcNow - DateTime.Now).TotalMilliseconds.ToString("#"),
                        Details = new EventDetail
                        {
                            Rating = new Rating
                            {
                                RatingValue = rating
                            }
                        },
                        EventId = Guid.NewGuid().ToString(),
                        TrackId = new MetaJamEventData
                        {
                            MetajamComapctKey = track.StoreId,
                        }
                    }
                }
            };
            var request = MakeRequest<RecordRealTime>();
            var data = await request.GetAsync(requestData);
            return data;
        }

        #endregion Other
    }
}