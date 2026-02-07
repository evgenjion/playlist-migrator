using Migrator.Core;
using Migrator.HTTPClient;

namespace Migrator.Repositories
{
    public sealed class TrackRepository : BaseSpotifyRepository
    {
        private static string _myTracksUriPath => "/v1/me/tracks";

        public TrackRepository(Config config, MyInterceptHttpClient httpClient)
            : base(config, httpClient, _myTracksUriPath) { }

        /// <summary>
        /// Get tracks from the url.
        /// </summary>
        /// <param name="uri">Full URL to playlist. With domain, path, and protocol. https://example.domain.com/path/123/item</param>
        /// <returns></returns>
        public async IAsyncEnumerable<TrackWrapperDTO> GetTracksForPlaylistByUri(
            string token,
            string url
        )
        {
            Init(token);

            var asyncGenerator = BaseGetPaginatedByFullUri<TrackWrapperDTO>(url);

            await foreach (var track in asyncGenerator)
            {
                yield return track;
            }
        }

        public async IAsyncEnumerable<TrackWrapperDTO> GetTracksForTheCurrentUser(string token)
        {
            Init(token);

            var asyncGenerator = BaseGetPaginatedDataByPath<TrackWrapperDTO>(
                _myTracksUriPath,
                _config.SpotifyGetMyTracksLimit
            );

            await foreach (var track in asyncGenerator)
            {
                yield return track;
            }
        }
    }

    public sealed class TrackWrapperDTO
    {
        public required TrackDTO Track { get; set; }
    }

    public sealed class TrackDTO
    {
        public required string Id { get; set; }
        public required string Name { get; set; }

        /// <summary>
        /// Used to add track into playlist in the future
        /// </summary>
        public required string Uri { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is TrackDTO other)
            {
                return Id == other.Id;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? (new Random()).Next();
        }
    }
}
