using System.Diagnostics.CodeAnalysis;
using Migrator.Core;
using Migrator.HTTPClient;
using Sprache;

namespace Migrator.Repositories
{
    public class PlaylistsRepository : BaseSpotifyRepository
    {
        private static string _myPlaylistsUriPath => "/v1/me/playlists";

        private static string _playlistPlaceholder = "<PLAYLIST_PLACEHOLDER>";
        private static string _playlistItemsUriPath =>
            $"/v1/playlists/{_playlistPlaceholder}/items";

        public PlaylistsRepository(Config config, MyInterceptHttpClient httpClient)
            : base(config, httpClient, _myPlaylistsUriPath) { }

        public async IAsyncEnumerable<PlaylistDTO> GetPaginatedData(string token)
        {
            Init(token);

            var paginatedGenerator = BaseGetPaginatedDataByPath<PlaylistDTO>();

            await foreach (var chunk in paginatedGenerator)
            {
                yield return chunk;
            }
        }

        public async Task<PlaylistDTO> CreatePlaylist(
            string token,
            PlaylistCreateDTO playlistToCreate
        )
        {
            Init(token);

            var result = await BaseCreateEntity<PlaylistCreateDTO, PlaylistDTO>(
                _myPlaylistsUriPath,
                playlistToCreate
            );

            return result;
        }

        public async Task<PlaylistDTO> CreatePlaylistWithName(
            string token,
            string name,
            string? description = null
        )
        {
            Init(token);

            PlaylistCreateDTO playlistToCreate = new(name, description);
            var result = await CreatePlaylist(token, playlistToCreate);

            return result;
        }

        public async Task AddTracks(PlaylistDTO playlist, List<TrackDTO> tracks)
        {
            // URL:
            // /v1/playlists/<HASH>/items
            var uri = _playlistItemsUriPath.Replace(_playlistPlaceholder, playlist.Id);
            var trackUris = tracks.Select(t => t.Uri).ToList();
            TrackAddDTO payload = new() { Position = 0, Uris = trackUris };

            var result = BaseCreateEntity<TrackAddDTO, TrackAddResponseDTO>(uri, payload);
        }
    }

    public class TrackAddResponseDTO
    {
        public string? SnapshotId { get; set; }
    }

    public class TrackAddDTO
    {
        public required int Position { get; set; }

        /// <summary>
        /// comma-separated track uris
        /// </summary>
        public required List<string> Uris { get; set; }
    }

    public class PlaylistEntityLinkDTO
    {
        public required string Href { get; set; }
        public required int Total { get; set; }
    }

    public class PlaylistDTO
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }

        [Obsolete("Use Items instead")]
        public required PlaylistEntityLinkDTO Tracks { get; set; }

        public required PlaylistEntityLinkDTO Items { get; set; }
    }

    public class PlaylistCreateDTO
    {
        public PlaylistCreateDTO() { }

        [SetsRequiredMembers]
        public PlaylistCreateDTO(string name, string? description = null)
        {
            Name = name;
            Public = false;
            Description = description;
        }

        public required string Name { get; set; }
        public required bool Public { get; set; }
        public string? Description { get; set; }
    }
}
