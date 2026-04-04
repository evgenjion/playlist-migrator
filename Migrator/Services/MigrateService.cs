using Migrator.Core;
using Migrator.Repositories;

namespace Migrator.Services
{
    class MigrateService
    {
        readonly AuthRepository _authRepository;
        readonly PlaylistsRepository _playlistsRepository;
        readonly TrackRepository _trackRepository;

        readonly Config _config;

        public MigrateService(
            AuthRepository authRepository,
            PlaylistsRepository playlistsRepository,
            TrackRepository trackRepository,
            Config config
        )
        {
            _authRepository = authRepository;
            _playlistsRepository = playlistsRepository;
            _trackRepository = trackRepository;
            _config = config;
        }

        /// <summary>
        /// Main entrypoint into migration process
        /// </summary>
        /// <param name="code">Code from /callback from Spotify</param>
        /// <returns></returns>
        public async Task StartMigration(string code)
        {
            var bearer = await getBearerFromCode(code);
            var allPlaylists = (await GetAllPlaylistsList(bearer)).ToList();

            PlaylistDTO destinationPlaylist = await GetOrCreateTargetPlaylist(bearer, allPlaylists);

            HashSet<TrackDTO> tracksInTargetPlaylist = (
                await GetAllTracksForPlaylist(bearer, destinationPlaylist)
            ).ToHashSet();

            var currentUserTracks = await GetCurrentUserTracks(bearer);
            var playlistsToMerge = allPlaylists
                .Where(p => p.Id != destinationPlaylist.Id)
                .ToList();

            foreach (PlaylistDTO sourcePlaylist in playlistsToMerge)
            {
                await AddTracksFromPlaylistToTargetPlaylist(
                    bearer,
                    sourcePlaylist,
                    destinationPlaylist,
                    tracksInTargetPlaylist
                );
            }

            await AddTracksToTargetPlaylist(
                destinationPlaylist,
                currentUserTracks,
                tracksInTargetPlaylist
            );
        }

        private async Task AddTracksToTargetPlaylist(
            PlaylistDTO targetPlaylist,
            List<TrackDTO> tracksInSourcePlaylist,
            HashSet<TrackDTO> tracksInTargetPlaylist
        )
        {
            List<TrackDTO> tracksToAdd = [];
            var scheduledTracks = new HashSet<TrackDTO>();

            foreach (var track in tracksInSourcePlaylist)
            {
                if (tracksInTargetPlaylist.Contains(track) || scheduledTracks.Contains(track))
                {
                    continue;
                }

                tracksToAdd.Add(track);
                scheduledTracks.Add(track);
            }

            var successfullyUploadedTracks = await UploadTracksIntoTargetPlaylistAsync(tracksToAdd, targetPlaylist);
            foreach (var track in successfullyUploadedTracks)
            {
                tracksInTargetPlaylist.Add(track);
            }
        }

        private async Task<List<TrackDTO>> GetCurrentUserTracks(string bearer)
        {
            List<TrackDTO> currentUserTracks = [];

            var tracksGenerator = _trackRepository.GetTracksForTheCurrentUser(bearer);
            await foreach (var trackWrapper in tracksGenerator)
            {
                currentUserTracks.Add(trackWrapper.Track);
            }

            return currentUserTracks;
        }

        private async Task AddTracksFromPlaylistToTargetPlaylist(
            string bearer,
            PlaylistDTO playlistFrom,
            PlaylistDTO playlistTo,
            HashSet<TrackDTO> tracksInTargetPlaylist
        )
        {
            List<TrackDTO> tracksFromPlaylist = await GetAllTracksForPlaylist(bearer, playlistFrom);

            List<TrackDTO> tracksToAdd = [];
            var scheduledTracks = new HashSet<TrackDTO>();
            foreach (var track in tracksFromPlaylist)
            {
                if (!tracksInTargetPlaylist.Contains(track) && !scheduledTracks.Contains(track))
                {
                    tracksToAdd.Add(track);
                    scheduledTracks.Add(track);
                    Console.WriteLine(
                        $"=============Scheduling addition of track {track.Id} into target playlist============="
                    );
                }
            }

            var successfullyUploadedTracks = await UploadTracksIntoTargetPlaylistAsync(tracksToAdd, playlistTo);
            foreach (var track in successfullyUploadedTracks)
            {
                tracksInTargetPlaylist.Add(track);
            }
        }

        private async Task<List<TrackDTO>> UploadTracksIntoTargetPlaylistAsync(
            List<TrackDTO> tracksToAdd,
            PlaylistDTO targetPlaylist
        )
        {
            var successfullyUploaded = new List<TrackDTO>();
            var tracksToAddRespectingLimit = tracksToAdd.Chunk(_config.SpotifyAddTracksLimit);

            foreach (var tracksChunk in tracksToAddRespectingLimit)
            {
                try
                {
                    await Task.Delay(_config.SpotifyDelayBetweenRequestsInMs);
                    await _playlistsRepository.AddTracks(targetPlaylist, tracksChunk.ToList());
                    successfullyUploaded.AddRange(tracksChunk);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to upload chunk of {tracksChunk.Length} tracks: {ex.Message}");
                }
            }

            return successfullyUploaded;
        }

        private async Task<PlaylistDTO> GetOrCreateTargetPlaylist(
            string bearer,
            List<PlaylistDTO> allPlaylists
        )
        {
            var targetPlaylist = allPlaylists.Find(x => x.Name == _config.MergedPlaylistName);
            if (targetPlaylist == null)
            {
                targetPlaylist = await _playlistsRepository.CreatePlaylistWithName(
                    bearer,
                    _config.MergedPlaylistName,
                    _config.MergedPlaylistDescription
                );
            }

            return targetPlaylist;
        }

        private async Task<List<TrackDTO>> GetAllTracksForPlaylist(
            string bearer,
            PlaylistDTO playlist,
            int? limit = null
        )
        {
            var link = playlist.Items.Href;

            var tracks = _trackRepository.GetTracksForPlaylistByUri(bearer, link);

            if (limit != null)
            {
                tracks = tracks.Take(limit.Value);
            }

            var trackDTOs = await tracks.Select(x => x.Track).ToListAsync();

            return trackDTOs;
        }

        private async Task<IEnumerable<PlaylistDTO>> GetAllPlaylistsList(
            string bearer,
            int? limit = null
        )
        {
            var paginatedData = _playlistsRepository.GetPaginatedData(bearer);

            if (limit != null)
            {
                paginatedData = paginatedData.Take(limit.Value);
            }

            List<PlaylistDTO> result = [];
            await foreach (var playlist in paginatedData)
            {
                result.Add(playlist);
            }

            return result;
        }

        private async Task<string> getBearerFromCode(string code)
        {
            return await _authRepository.GetBearerTokenFromCallbackData(code);
        }
    }
}
