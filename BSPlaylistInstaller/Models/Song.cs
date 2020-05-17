using Newtonsoft.Json;

namespace BSPlaylistInstaller.Models
{
    public class Song
    {
        [JsonConstructor]
        public Song(string hash)
        {
            Key = string.Empty;
            Hash = hash;
            SongName = string.Empty;
            Uploader = string.Empty;
        }

        public Song(string key, string hash, string songName, string uploader)
        {
            Key = key;
            Hash = hash;
            SongName = songName;
            Uploader = uploader;
        }

        public string Key { get; set; }
        public string Hash { get; set; }
        public string SongName { get; set; }
        public string Uploader { get; set; }
    }
}