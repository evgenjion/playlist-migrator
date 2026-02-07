## About
Tool that merges all tracks from the Spotify account into a single playlist

## How to use
`GET` https://localhost:5001/api/v1/oauth/login
follow redirect to spotify, oauth in browser, get redirected back
`GET` https://localhost:5001/api/v1/oauth/callback?code=CODE_FROM_SPOTIFY\\

(migration begins when /callback called with proper code)
