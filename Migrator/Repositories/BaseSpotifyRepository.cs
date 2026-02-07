using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Migrator.Core;

namespace Migrator.Repositories
{
    public abstract class BaseSpotifyRepository
    {
        private int _limit;

        private bool _inited = false;

        protected readonly HttpClient _httpClient;

        private readonly string _defaultUriPath;
        protected Config _config;

        public BaseSpotifyRepository(Config config, HttpClient httPClient, string uriPath)
        {
            _httpClient = httPClient;
            _defaultUriPath = uriPath;
            _config = config;

            _httpClient.BaseAddress = new Uri(config.SpotifyApiUri);
            _limit = config.SpotifyBaseRequestLimit;
        }

        public void Init(string token)
        {
            if (_inited)
            {
                return;
            }

            _inited = true;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token
            );
        }

        protected async IAsyncEnumerable<T> BaseGetPaginatedByFullUri<T>(
            string fullUri,
            int? overrideLimit = null
        )
        {
            var uri = new Uri(fullUri);
            var path = uri.AbsolutePath;

            await foreach (var chunk in BaseGetPaginatedDataByPath<T>(path, overrideLimit))
            {
                yield return chunk;
            }
        }

        protected async IAsyncEnumerable<T> BaseGetPaginatedDataByPath<T>(
            string? path = null,
            int? overrideLimit = null
        )
        {
            int total;
            int totalData;
            int page = 0;
            int limit = _limit;

            if (overrideLimit != null)
            {
                limit = overrideLimit.Value;
            }

            do
            {
                var chunk = await BaseGetDataChunk<T>(page, path, overrideLimit);
                totalData = chunk.Total;

                total = limit * page + limit; // full pages count + current page
                ++page;

                foreach (var item in chunk.Items)
                {
                    yield return item;
                }
            } while (total < totalData);

            yield break;
        }

        protected async Task<SpotifyResponseDTO<T>> BaseGetDataChunk<T>(
            int page,
            string? path,
            int? overrideLimit = null
        )
        {
            int limit = _limit;
            if (overrideLimit != null)
            {
                limit = overrideLimit.Value;
            }

            var queryParams = new Dictionary<string, string?>() { ["limit"] = limit.ToString() };

            if (page > 0)
            {
                queryParams["offset"] = (page * limit).ToString();
            }

            string fullUri = QueryHelpers.AddQueryString(path ?? _defaultUriPath, queryParams);
            using HttpResponseMessage playListsResponse = await _httpClient.GetAsync(fullUri);
            playListsResponse.EnsureSuccessStatusCode();

            string jsonResponse = await playListsResponse.Content.ReadAsStringAsync();
            var response = BaseGetDataFromJSON<T>(jsonResponse);

            return response;
        }

        protected SpotifyResponseDTO<T> BaseGetDataFromJSON<T>(string jsonString)
        {
            using JsonDocument document = JsonDocument.Parse(jsonString);
            var serializeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                SpotifyResponseDTO<T> spotifyResponse = JsonSerializer.Deserialize<
                    SpotifyResponseDTO<T>
                >(jsonString, serializeOptions)!;

                return spotifyResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"-----ex-----> {ex}");
                throw new Exception("Error during parsing Spotify playlist response");
            }
        }

        protected async Task<TResult> BaseCreateEntity<TCreate, TResult>(
            string uriPath,
            TCreate entity
        )
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            };
            HttpContent requestContent = JsonContent.Create(entity, options: options);

            HttpResponseMessage createPlaylistResponse = await _httpClient.PostAsync(
                uriPath,
                requestContent
            );

            createPlaylistResponse.EnsureSuccessStatusCode();
            var responseBody = await createPlaylistResponse.Content.ReadFromJsonAsync<TResult>();

            if (responseBody == null)
            {
                throw new InvalidDataException("Couldn't parse response from spotify");
            }

            return responseBody;
        }

        public class SpotifyResponseDTO<T>
        {
            public int Total { get; set; }

            public required List<T> Items { get; set; }
        }
    }
}
