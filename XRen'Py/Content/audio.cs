﻿using System;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Input;
using AlbumArtExtraction;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace X_Ren_Py
{
    public partial class MainWindow : Window
    {
		public class XAudio : XContent
		{
            public string Type { get; set; } = "music ";

            public XAudio() { ContextMenu = cmAudio; }
			public void loadAudio(string singleLine, string folder)
			{
				try
				{
					int firstquote = singleLine.IndexOf('"') + 1;
					Path = folder + singleLine.Substring(firstquote, singleLine.LastIndexOf('"') - firstquote);
					Header = Path.Substring(singleLine.LastIndexOf('/') + 1);
					Alias = singleLine.Substring(13, singleLine.IndexOf('=') - 13).TrimEnd(' ');
				}
				catch (Exception) { MessageBox.Show("Error: Audio loading", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
			}
		}

		public class AudioProperties : ContentProperties
		{
            public XAudio Audio { get; set; }
            public float FadeIn { get; set; }
            public float FadeOut { get; set; }
            //public bool Queue { get { return _Queue; } set { _Queue = value; } }
            public bool Loop { get; set; } = false;
        }

		private void audioImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog musDialog = new OpenFileDialog() { Filter = audioextensions, Multiselect = true };

            if (musDialog.ShowDialog() == true)
                for (int file = 0; file < musDialog.FileNames.Length; file++)
                {
                    try
					{
						string name = musDialog.SafeFileNames[file];
						if (!(tabControlResources.SelectedContent as ListView).Items.OfType<XAudio>().Any(item => item.Header == name))
						{
							XAudio audio = new XAudio() { Header = name };
							audioMouseActions(audio);

							if (tabControlResources.SelectedContent == musicListView)
							{
								audio.Type = "music "; musicListView.Items.Add(audio);
								audio.Path = projectFolder + musicFolder + name;
							}
							else if (tabControlResources.SelectedContent == soundListView)
							{
								audio.Type = "sound "; soundListView.Items.Add(audio);
								audio.Path = projectFolder + soundsFolder  + name;
							}
							else
							{
								audio.Type = "voice "; voiceListView.Items.Add(audio);
								audio.Path = projectFolder + voicesFolder + name;
							}

							contentCollector(musDialog.FileNames[file], audio.Path);
						}
						else MessageBox.Show("Audio is already in use!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
					catch (Exception) { MessageBox.Show("Please choose the audio!", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
        }

		private void audioReload_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog musDialog = new OpenFileDialog() { Filter = audioextensions };
			if (musDialog.ShowDialog() == true)
				try
				{
					currentAudio.Header = musDialog.SafeFileName;
					currentAudio.Path = musDialog.FileName;
					coverartShow(musDialog.FileName);
					movieplayer.Source = new Uri(musDialog.FileName, UriKind.Absolute);
					currentAudio.TextColor = Brushes.Black;
					currentAudio.Checkbox.IsEnabled = true;
				}
				catch (Exception)
				{
					MessageBox.Show("Invalid audio", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
		}

		private void coverartShow(string audiofile)
		{
			ID3v2AlbumArtExtractor extractID3 = new ID3v2AlbumArtExtractor();
			ID3v22AlbumArtExtractor extractID3V2 = new ID3v22AlbumArtExtractor();

			BitmapImage bitmapToShow = new BitmapImage();
			bitmapToShow.BeginInit();
			bitmapToShow.CacheOption = BitmapCacheOption.OnLoad;

				if (extractID3V2.CheckType(audiofile) && extractID3V2.Extract(audiofile) != null)
					bitmapToShow.StreamSource = extractID3V2.Extract(audiofile);
				else if (extractID3.CheckType(audiofile) && extractID3.Extract(audiofile) != null)
					bitmapToShow.StreamSource = extractID3.Extract(audiofile);
				else bitmapToShow.UriSource = new Uri("Images/Music.png", UriKind.Relative);

			bitmapToShow.EndInit();
			coverart.Source = bitmapToShow;
		}

		private void audioMouseActions(XAudio newaudio)
		{
			newaudio.Selected += content_Selected;
			newaudio.MouseUp += content_Selected;
			newaudio.MouseLeave += audio_MouseLeave;
			newaudio.MouseEnter += audiomovie_Enter;
			newaudio.Checkbox.Checked += audio_Checked;
			newaudio.Checkbox.Unchecked += audio_Unchecked;
			newaudio.Checkbox.Indeterminate += audio_Indeterminate;
		}

		private void audio_MouseLeave(object sender, MouseEventArgs e)
		{
			if (!show)
				media.IsExpanded = false;
			else
			{
				audioPropsPanel.Visibility = Visibility.Collapsed;
				imagePropsPanel.Visibility = Visibility.Collapsed;
				if (currentAudio != null)
				{					
					movieplayer.Source = new Uri(currentAudio.Path, UriKind.Absolute);
					mediaNameLabel.Content = currentAudio.Header;
					coverartShow(currentAudio.Path);
					if (currentAudio.IsChecked == true)
					{
						getAudioProperties(currentAudio);
						audioPropsPanel.Visibility = Visibility.Visible;
					}
					else audioPropsPanel.Visibility = Visibility.Hidden;
				}
			}
		}

		private void audio_Checked(object sender, RoutedEventArgs e)
        {						
            currentAudio = (sender as CheckBox).Tag as XAudio;
			bool isLooped=false;

			if (AudioInFrameProps.Any(prop => (prop.StopFrame == currentFrame || (prop.StopFrames != null && prop.StopFrames.Intersect(previousFrames) == currentFrame)) && prop.Audio == currentAudio))
			{
				AudioProperties audio = AudioInFrameProps.First(prop => (prop.StopFrame == currentFrame || (prop.StopFrames != null && prop.StopFrames.Intersect(previousFrames) == currentFrame)) && prop.Audio == currentAudio);
				if (audio.StopFrame == currentFrame) audio.StopFrame = null; else audio.StopFrames.Remove(currentFrame);
				(sender as CheckBox).IsChecked = null;
			}
			else
			{
				if (currentAudio.Type == "music ")
				{
					isLooped = true;
					selectOnlyCurrentaudio(lastMusicChecked, currentAudio);
					lastMusicChecked = currentAudio;					
					addAudioToLayer(currentAudio.Path, music, panelMusic, labelMusic);
				}
				else if (currentAudio.Type == "sound ")
				{
					selectOnlyCurrentaudio(lastSoundChecked, currentAudio);
					lastSoundChecked = currentAudio;
					addAudioToLayer(currentAudio.Path, sound, panelSound, labelSound);
				}
				else
				{
					selectOnlyCurrentaudio(lastVoiceChecked, currentAudio);
					lastVoiceChecked = currentAudio;
					addAudioToLayer(currentAudio.Path, voice, panelVoice, labelVoice);
				}

				if (addorselect) AudioInFrameProps.Add(new AudioProperties() { Frame = currentFrame, Audio = currentAudio, Loop = isLooped });
				getAudioProperties(currentAudio);
			}

			audioPropsPanel.Visibility = Visibility.Visible;
			show = true;
			waschecked = true;
		}

		private void audio_Indeterminate(object sender, RoutedEventArgs e)
		{
			if (waschecked && addorselect) (sender as CheckBox).IsChecked = false;
			else
			{
				XAudio selectedAudio = (sender as CheckBox).Tag as XAudio;
				if (selectedAudio.Type == "music ")	addAudioToLayer(selectedAudio.Path, music, panelMusic, labelMusic);
				else if (selectedAudio.Type == "sound ") addAudioToLayer(selectedAudio.Path, sound, panelSound, labelSound);
				else addAudioToLayer(selectedAudio.Path, voice, panelVoice, labelVoice);
			};
		}

		private void audio_Unchecked(object sender, RoutedEventArgs e)
		{
			XAudio selectedAudio = (sender as CheckBox).Tag as XAudio;
			if (waschecked)
			{
				if (removeorunselect) AudioInFrameProps.Remove(AudioInFrameProps.Find(i => i.Frame == currentFrame && i.Audio == selectedAudio));
				waschecked = false;
			}
			else
			{
				AudioProperties audio = AudioInFrameProps.Last(i => previousFrames.Contains(i.Frame) && i.Audio == selectedAudio);
				if (removeorunselect) if (audio.Frame.MenuOptions == null) audio.StopFrame = currentFrame; else audio.StopFrames.Add(currentFrame);
			}

			if (selectedAudio.Type == "music ")	hideAudioLayer(music, panelMusic, labelMusic);
			else if (selectedAudio.Type == "sound ") hideAudioLayer(sound, panelSound, labelSound);
			else hideAudioLayer(voice, panelVoice, labelVoice);

			audioPropsPanel.Visibility = Visibility.Hidden;
			show = false;
		}

		private void addAudioToLayer(string path, MediaElement audio, StackPanel panel, Label label)
		{
			audio.Source = new Uri(path);
			panel.Visibility = Visibility.Visible;
			label.Content = currentAudio.Header;
		}

		private void hideAudioLayer(MediaElement audio, StackPanel panel, Label label)
		{
			audio.Source = null;
			panel.Visibility = Visibility.Collapsed;
			label.Content = null;
		}

		private void audioDeleteFromList_Click(object sender, RoutedEventArgs e)
        {
            foreach (AudioProperties audio in AudioInFrameProps.Where(audio => audio.Audio == sender)) AudioInFrameProps.Remove(audio);
            resourcesSelectedItem_delete();
        }

		public void getAudioProperties(XAudio audio)
		{
			AudioProperties property = AudioInFrameProps.Find(prop => prop.Frame == currentFrame && prop.Audio == audio);
			fadeinTextBox.Text = property.FadeIn.ToString();
			fadeoutTextBox.Text = property.FadeOut.ToString();
			//queueCheckBox.IsChecked = property.Queue;
			loopCheckBox.IsChecked = property.Loop;
			imagePropsPanel.Visibility = Visibility.Hidden;
		}

		private void audioMixer_Click(object sender, RoutedEventArgs e)
		{
			if (mixerButton.Background == Brushes.LightBlue)
			{
				mixerButton.Background = new SolidColorBrush(Color.FromArgb(102, 255, 255, 255));
				mixerGrid.Visibility = Visibility.Collapsed;
			}
			else
			{
				mixerButton.Background = Brushes.LightBlue;
				mixerGrid.Visibility = Visibility.Visible;
			}
		}

		private void selectOnlyCurrentaudio(XAudio previous, XAudio current)
		{
			if (previous != null && previous != current)
			{
				if (addorselect) AudioInFrameProps.Last(prop => prop.Audio == previous && previousFrames.Contains(prop.Frame)).StopFrame = currentFrame;
				previous.IsChecked = false;
			}
		}
	}
}
