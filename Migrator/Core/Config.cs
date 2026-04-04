namespace Migrator.Core
{
    public class Config
    {
        public string? ConnectionString { get; set; }

        public string ClientId =>
            Environment.GetEnvironmentVariable("CLIENT_ID")
            ?? throw new Exception("CLIENT_ID environment variable not set");
        public string ClientSecret =>
            Environment.GetEnvironmentVariable("CLIENT_SECRET")
            ?? throw new Exception("CLIENT_SECRET environment variable not set");
        public string SpotifyApiUri => "https://api.spotify.com";
        public string AccountsUri => "https://accounts.spotify.com";

        /// <summary>
        /// URL to spotify to get user's credentials and be redirected to a callback
        /// </summary>
        public string SpotifyLoginUri => "https://accounts.spotify.com/authorize";

        /// <summary>
        /// URL to spotify to get user's credentials _after_ /callback is called
        /// </summary>
        public string SpotifyTokenUri => "https://accounts.spotify.com/api/token";

        private string SpotifyUserPersonalTracksReadPermission => "user-library-read";
        private string SpotifyCreateAndUpdatePlaylistPermission =>
            "playlist-modify-public playlist-modify-private";
        public string SpotifyOAuthScope =>
            $"user-read-private {SpotifyCreateAndUpdatePlaylistPermission} {SpotifyUserPersonalTracksReadPermission}";

        public string SpotifyRedirectUri => "https://127.0.0.1/api/v1/oauth/callback";
        // public string MergedPlaylistName => "[Merged playlists]";
        public string MergedPlaylistName => "[All in One v2]";
        public string MergedPlaylistDescription => "Playlist created by the playlist mergify tool";
        public int SpotifyBaseRequestLimit = 100;
        public int SpotifyGetMyTracksLimit = 50;
        public int SpotifyAddTracksLimit = 100;
        public int SpotifyDelayBetweenRequestsInMs = 200;
        public int SpotifyRequestTimeoutInMs = 10_000;
    }
}
