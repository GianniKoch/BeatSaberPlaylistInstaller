using Newtonsoft.Json;

namespace BSPlaylistInstaller.Models
{
	public struct Song
	{
		[JsonConstructor]
		public Song(string key, string hash, string songName, string uploader)
		{
			Key = key;
			Hash = hash;
			SongName = songName;
			Uploader = uploader;
		}

		public string Key { get; }
		public string Hash { get; }
		public string SongName { get; }
		public string Uploader { get; }
	}
}