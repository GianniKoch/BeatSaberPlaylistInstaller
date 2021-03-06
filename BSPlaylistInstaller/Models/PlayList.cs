﻿using System.Collections.Generic;

namespace BSPlaylistInstaller.Models
{
    public class PlayList
    {
        public PlayList()
        {
            Songs = new List<Song>();
        }

        public PlayList(string playlistTitle, string playlistAuthor, string image, List<Song> songs)
        {
            PlaylistTitle = playlistTitle;
            PlaylistAuthor = playlistAuthor;
            Image = image;
            Songs = songs;
        }

        public string PlaylistTitle { get; set; }
        public string PlaylistAuthor { get; set; }
        public string Image { get; set; }
        public List<Song> Songs { get; set; }
    }
}