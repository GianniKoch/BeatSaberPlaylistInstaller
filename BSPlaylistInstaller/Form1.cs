using System;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.IO;
using BeatSaverSharp;
using System.IO.Compression;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;

namespace BSPlaylistInstaller
{
    public partial class Form1 : Form
    {
        private List<string> playlistdownloading;
        public Form1()
        {
            InitializeComponent();
            playlistdownloading = new List<string>();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog choofdlog = new OpenFileDialog();
                choofdlog.Filter = "Beat Saber Playlist(*.json; *.bplist)| *.json; *.bplist";
                choofdlog.Multiselect = false;

                if (choofdlog.ShowDialog() == DialogResult.OK)
                {
                    string sFileName = choofdlog.FileName;
                    int random = new Random().Next();
                    cboActiveStop.Items.Add(choofdlog.SafeFileName + random.ToString());
                    var playlist = choofdlog.SafeFileName + random.ToString();
                    playlistdownloading.Add(playlist);
                    txtLog.Text = "new path selected: " + sFileName + Environment.NewLine + txtLog.Text;

                    MessageBox.Show("Select your main beatsaver folder (folder with the BeatSaber.exe)");
                    string bsfolderpath;
                    using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
                    {
                        dialog.IsFolderPicker = true;
                        dialog.Multiselect = false;
                        if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                        {
                            txtLog.Text = "No beatsaber folder found!" + Environment.NewLine + txtLog.Text;
                            return;
                        }
                        bsfolderpath = dialog.FileName;
                    }

                    txtLog.Text = "new bs folder path selected: " + bsfolderpath + Environment.NewLine + txtLog.Text;

                    JObject data = JObject.Parse(File.ReadAllText(sFileName));
                    var array = data["songs"];

                    JArray a = JArray.Parse(array.ToString());
                    var songs = a.Count;
                    var options = new HttpOptions
                    {
                        ApplicationName = "Beatsaber Playlist Missing Songs Downloader",
                        Version = new Version(1, 0, 0),
                        HandleRateLimits = true,
                    };
                    BeatSaver bs = new BeatSaver(options);
                    for (var i = 0; i < songs; i++)
                    {
                        if (!playlistdownloading.Contains(playlist))
                        {
                            BeginInvoke((MethodInvoker)delegate
                            {
                                txtLog.Text = $"Stopped downloading {playlist}" + Environment.NewLine + txtLog.Text;
                                cboActiveStop.Items.Remove(playlist);
                                cboActiveStop.Text = "";
                            });
                            return;
                        }
                        try
                        {   
                            string hash = a[i]["hash"].ToString();
                            var beatmapje = await bs.Hash(hash);
                            var excist = false;
                            foreach(var file in Directory.GetDirectories($@"{ bsfolderpath}\Beat Saber_Data\CustomLevels"))
                            {
                                if (file.Contains(beatmapje.Key))
                                {
                                    BeginInvoke((MethodInvoker)delegate
                                    {
                                        txtLog.Text = $"{i + 1}/{songs} | {beatmapje.Name} already excists!" + Environment.NewLine + txtLog.Text;
                                    });
                                    excist = true;
                                }
                            }
                            if (excist)
                                continue;
                            var zipBytes = await beatmapje.DownloadZip();
                            var zippath = Path.Combine($@"{bsfolderpath}\Beat Saber_Data\CustomLevels", $"{beatmapje.Name}.zip");
                            using (var memStream = new MemoryStream(zipBytes))
                            {
                                using (var writer = File.OpenWrite(zippath))
                                {
                                    BeginInvoke((MethodInvoker)delegate
                                    {
                                        txtLog.Text = $"{i + 1}/{songs} | downloading {beatmapje.Name}" + Environment.NewLine + txtLog.Text;
                                    });
                                    await memStream.CopyToAsync(writer).ConfigureAwait(false);
                                    await writer.FlushAsync().ConfigureAwait(false);
                                }
                            }
                            using (var beatmapjezip = ZipFile.OpenRead(zippath))
                            {
                                var beatmapfolderpath = Path.Combine($@"{bsfolderpath}\Beat Saber_Data\CustomLevels", $"{beatmapje.Key} {beatmapje.Name} - {beatmapje.Uploader.Username}");
                                Directory.CreateDirectory(beatmapfolderpath);
                                BeginInvoke((MethodInvoker)delegate
                                {
                                    txtLog.Text = $"{i + 1}/{songs} | unzipping {beatmapje.Name}" + Environment.NewLine + txtLog.Text;
                                });
                                foreach (var entry in beatmapjezip.Entries)
                                {
                                    using (var ZipEntryStream = entry.Open())
                                    using (var writer = File.OpenWrite(Path.Combine(beatmapfolderpath, entry.Name)))
                                    {
                                        await ZipEntryStream.CopyToAsync(writer).ConfigureAwait(false);
                                        await writer.FlushAsync().ConfigureAwait(false);
                                    }
                                }
                            }
                            BeginInvoke((MethodInvoker)delegate
                            {
                                txtLog.Text = $"{i + 1}/{songs} | success downloading {beatmapje.Name} " + Environment.NewLine + txtLog.Text;
                            });
                            File.Delete(zippath);
                        }
                        catch(Exception ex)
                        {
                            BeginInvoke((MethodInvoker)delegate
                            {
                                txtLog.Text = $"{i}/{songs} | failed download " + 
                                Environment.NewLine + $"Message: {ex.Message}" + Environment.NewLine + txtLog.Text;
                            });
                        }
                    }
                    BeginInvoke((MethodInvoker)delegate
                    {
                        cboActiveStop.Items.Remove(playlist);
                        cboActiveStop.Text = "";
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            BeginInvoke((MethodInvoker)delegate
            {
                txtLog.Text = "Finished downloading missing songs from playlist!" + Environment.NewLine + txtLog.Text;
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
            catch (Exception ex)
            {
                return; // Nothing selected
            }
            
            if (!playlistdownloading.Contains(cboActiveStop.SelectedItem.ToString()))
            {
                MessageBox.Show("This playlist already stopped downloading!");
                return;
            }
            playlistdownloading.Remove(cboActiveStop.SelectedItem.ToString());
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            playlistdownloading = new List<string>();
        }
    }
}
