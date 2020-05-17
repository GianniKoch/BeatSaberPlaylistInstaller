using System;
using System.Windows.Forms;
using System.IO;
using BeatSaverSharp;
using System.IO.Compression;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSPlaylistInstaller.Models;
using Newtonsoft.Json;

namespace BSPlaylistInstaller
{
	public partial class Form1 : Form
	{
		private List<string> _playlistDownloading;
		private BeatSaver _beatSaver;

		public Form1()
		{
			InitializeComponent();
			_playlistDownloading = new List<string>();

			var options = new HttpOptions
			{
				ApplicationName = "Beatsaber Playlist Missing Songs Downloader",
				Version = new Version(1, 0, 0),
				HandleRateLimits = true,
			};
			_beatSaver = new BeatSaver(options);
		}

		private async void button1_Click(object sender, EventArgs e)
		{

			var choofdlog = new OpenFileDialog
			{
				Filter = "Beat Saber Playlist(*.json; *.bplist)| *.json; *.bplist",
				Multiselect = false
			};

			if (choofdlog.ShowDialog() != DialogResult.OK)
			{
				return;
			}

			var sFileName = choofdlog.FileName;
			var random = new Random().Next();
			cboActiveStop.Items.Add(choofdlog.SafeFileName + random);
			var playListName = choofdlog.SafeFileName + random;
			_playlistDownloading.Add(playListName);
			WriteToLog("New path selected: " + sFileName);

			if (SelectBeatSaberFolderPah(out var bsFolderPath))
			{
				return;
			}

			WriteToLog("New bs folder path selected: " + bsFolderPath);

			PlayList playList;
			try
			{
				playList = JsonConvert.DeserializeObject<PlayList>(File.ReadAllText(sFileName));
				playList.Songs = playList.Songs.Where(x => !string.IsNullOrWhiteSpace(x.Hash)).ToList();
			}
			catch (Exception ex)
			{
				WriteToLog($"Failed reading playlist{Environment.NewLine}" +
				           $"Message: {ex.Message}");
				return;
			}

			await DownloadPlayList(playList, playListName, bsFolderPath);

			BeginInvoke((MethodInvoker) delegate
			{
				cboActiveStop.Items.Remove(playListName);
				cboActiveStop.Text = "";
			});

		}

		private void btnStopDownload_Click(object sender, EventArgs e)
		{
			try
			{
				if (cboActiveStop.SelectedItem.ToString() == "")
				{
					return;
				}
			}
			catch (Exception)
			{
				// NOP
				return; // Nothing selected
			}

			if (!_playlistDownloading.Contains(cboActiveStop.SelectedItem.ToString()))
			{
				MessageBox.Show("This playlist already stopped downloading!");
				return;
			}

			_playlistDownloading.Remove(cboActiveStop.SelectedItem.ToString());
		}

		private void btnStopAll_Click(object sender, EventArgs e)
		{
			_playlistDownloading.Clear();
		}

		private bool SelectBeatSaberFolderPah(out string bsFolderPath)
		{
			MessageBox.Show("Select your main beatsaver folder (folder with the BeatSaber.exe)");
			bsFolderPath = null;

			using (var dialog = new CommonOpenFileDialog
			{
				IsFolderPicker = true,
				Multiselect = false
			})
			{
				if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
				{
					WriteToLog("No Beat Saber folder found!");
					return true;
				}

				bsFolderPath = dialog.FileName;

				return false;
			}
		}

		private async Task DownloadPlayList(PlayList playList, string playListName, string bsFolderPath)
		{
			var songs = playList.Songs.Count;
			for (var i = 0; i < songs; i++)
			{
				if (!_playlistDownloading.Contains(playListName))
				{
					BeginInvoke((MethodInvoker) (() =>
					{
						WriteToLog($"Stopped downloading {playListName}");
						cboActiveStop.Items.Remove(playListName);
						cboActiveStop.Text = "";
					}));
					return;
				}

				try
				{
					var beatMap = await _beatSaver.Hash(playList.Songs[i].Hash);
					if (CheckIfSongAlreadyPresent(bsFolderPath, beatMap))
					{
						WriteToLog($"{i + 1}/{songs} | {beatMap.Name} already exists!");
						continue;
					}

					await DownloadAndExtractBeatMap(beatMap, bsFolderPath, i, songs);
				}
				catch (Exception ex)
				{
					WriteToLog($"{i}/{songs} | failed download{Environment.NewLine}" +
					           $"Message: {ex.Message}");
				}
			}

			WriteToLog("Finished downloading missing songs from playlist!");
		}

		private async Task DownloadAndExtractBeatMap(Beatmap beatMap, string bsFolderPath, int songNumber, int totalSongs)
		{
			var zipBytes = await beatMap.DownloadZip();
			var zipPath = Path.Combine($@"{bsFolderPath}\Beat Saber_Data\CustomLevels", $"{beatMap.Name}.zip");
			using (var memStream = new MemoryStream(zipBytes))
			{
				using (var writer = File.OpenWrite(zipPath))
				{
					WriteToLog($"{songNumber + 1}/{totalSongs} | downloading {beatMap.Name}");

					await memStream.CopyToAsync(writer).ConfigureAwait(false);
					await writer.FlushAsync().ConfigureAwait(false);
				}
			}

			using (var beatMapZip = ZipFile.OpenRead(zipPath))
			{
				var beatMapFolderPath = Path.Combine($@"{bsFolderPath}\Beat Saber_Data\CustomLevels", $"{beatMap.Key} {beatMap.Name} - {beatMap.Uploader.Username}");
				Directory.CreateDirectory(beatMapFolderPath);
				WriteToLog($"{songNumber + 1}/{totalSongs} | unzipping {beatMap.Name}");

				foreach (var entry in beatMapZip.Entries)
				{
					using (var zipEntryStream = entry.Open())
					using (var writer = File.OpenWrite(Path.Combine(beatMapFolderPath, entry.Name)))
					{
						await zipEntryStream.CopyToAsync(writer).ConfigureAwait(false);
						await writer.FlushAsync().ConfigureAwait(false);
					}
				}
			}

			WriteToLog($"{songNumber + 1}/{totalSongs} | success downloading {beatMap.Name}");
			File.Delete(zipPath);
		}

		private void WriteToLog(string toLog)
		{
			BeginInvoke((MethodInvoker) (() => txtLog.Text = toLog + Environment.NewLine + txtLog.Text));
		}

		private static bool CheckIfSongAlreadyPresent(string bsFolderPath, Beatmap beatMap)
		{
			return Directory
				.GetDirectories($@"{bsFolderPath}\Beat Saber_Data\CustomLevels")
				.Any(folderName => Path.GetFileName(folderName).StartsWith(beatMap.Key));
		}
	}
}