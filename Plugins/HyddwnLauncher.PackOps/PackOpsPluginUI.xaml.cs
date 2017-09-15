﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HyddwnLauncher.Extensibility;
using HyddwnLauncher.Extensibility.Interfaces;
using HyddwnLauncher.PackOps.Core;
using HyddwnLauncher.PackOps.Pack;
using HyddwnLauncher.PackOps.Util;

namespace HyddwnLauncher.PackOps
{
    /// <summary>
    /// Interaction logic for PackOpsPluginUI.xaml
    /// </summary>
    public partial class PackOpsPluginUI : UserControl
    {
        private IClientProfile _clientProfile;
        private IServerProfile _serverProfile;
        private PluginContext _pluginContext;

		public static readonly DependencyProperty MaximumPackVersionProperty = DependencyProperty.Register(
			"MaximumPackVersion", typeof(int), typeof(PackOpsPluginUI), new PropertyMetadata(default(int)));

		public static readonly DependencyProperty MinimumPackVersionProperty = DependencyProperty.Register(
			"MinimumPackVersion", typeof(int), typeof(PackOpsPluginUI), new PropertyMetadata(default(int)));

		public static readonly DependencyProperty FromValueProperty = DependencyProperty.Register(
			"FromValue", typeof(int), typeof(PackOpsPluginUI), new PropertyMetadata(default(int)));

		public static readonly DependencyProperty ToValueProperty = DependencyProperty.Register(
			"ToValue", typeof(int), typeof(PackOpsPluginUI), new PropertyMetadata(default(int)));

		public PackOpsPluginUI(PluginContext pluginContext, IClientProfile clientProfile, IServerProfile serverProfile)
        {
            _pluginContext = pluginContext;
            _clientProfile = clientProfile;
            _serverProfile = serverProfile;

			PackOpsSettings = PackOpsSettingsManager.Instance.PackOpsSettings;

            PackViewEntries = new ObservableCollection<PackViewerEntry>();
            PackOperationsViewModels = new ObservableCollection<PackOperationsViewModel>();

            InitializeComponent();
        }

		public int MaximumPackVersion
		{
			get { return (int)GetValue(MaximumPackVersionProperty); }
			set { SetValue(MaximumPackVersionProperty, value); }
		}

		public int MinimumPackVersion
		{
			get { return (int)GetValue(MinimumPackVersionProperty); }
			set { SetValue(MinimumPackVersionProperty, value); }
		}

		public int FromValue
		{
			get { return (int)GetValue(FromValueProperty); }
			set { SetValue(FromValueProperty, value); }
		}

		public int ToValue
		{
			get { return (int)GetValue(ToValueProperty); }
			set { SetValue(ToValueProperty, value); }
		}

		public PackOpsSettings PackOpsSettings { get; protected set; }
		public List<PackListEntry> PackFileEntries { get; private set; }
        public ObservableCollection<PackViewerEntry> PackViewEntries { get; private set; }
        public ObservableCollection<PackOperationsViewModel> PackOperationsViewModels { get; private set; }

        public void ClientProfileChangedAsync(IClientProfile clientProfile)
        {
            _clientProfile = clientProfile;

			GetPacksForClientProfile();
        }

        private async void GetPacksForClientProfile()
        {
            if (_clientProfile == null) return;

            PackOperationsViewModels.Clear();

			await Task.Run(() =>
			{
				foreach (var packFilePath in Directory.EnumerateFiles($"{Path.GetDirectoryName(_clientProfile.Location)}\\package", "*.pack", SearchOption.TopDirectoryOnly).OrderBy(a => a))
					Dispatcher.Invoke(() => PackOperationsViewModels.Add(new PackOperationsViewModel(packFilePath)));

				var packViewModelWorkingSet = PackOperationsViewModels.Where(pvm => pvm.IsSequenceTargetable).OrderBy(pvm => pvm.PackVersion).ToList();

				Dispatcher.Invoke(() =>
				{
					MaximumPackVersion = packViewModelWorkingSet.LastOrDefault().PackVersion;
					MinimumPackVersion = packViewModelWorkingSet.FirstOrDefault().PackVersion;
				});
			});
        }

        public void ServerProfileChanged(IServerProfile serverProfile)
        {
            _serverProfile = serverProfile;

			GetPacksForClientProfile();
        }

        private async void PackViewerRefreshOnClick(object sender, RoutedEventArgs e)
        {
			PackViewLoader.IsOpen = true;
			await Task.Delay(500);

			await GetPackEntries();
            await Refresh();
        }

        private async Task GetPackEntries()
        {
            if (_clientProfile == null) return;

            var packagePath = $"{Path.GetDirectoryName(_clientProfile.Location)}\\package";

			await Task.Run(() =>
			{
				using (var packReader = new PackReader(packagePath))
					PackFileEntries = packReader.GetEntries().OrderBy(g => g.PackFilePath).ToList();
			});
        }

        private async Task Refresh()
        {
            if (_clientProfile == null) return;
            if (string.IsNullOrWhiteSpace(_clientProfile.Location)) return;

            await Populate();

            PackViewLoader.IsOpen = false;
        }

        private async Task Populate()
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    PackViewEntries.Clear();
                    PackViewTreeView.Items.SortDescriptions.Clear();

                    foreach (var packFileEntry in PackFileEntries)
                    {
                        PackViewerEntry root =
                            PackViewEntries.FirstOrDefault(
                                x => x.Name.Equals(Path.GetFileName(packFileEntry.PackFilePath)) && x.Level.Equals(1));
                        if (root == null)
                        {
                            root = new PackViewerEntry
                            {
                                Level = 1,
                                Name = Path.GetFileName(packFileEntry.PackFilePath),
                            };
                            PackViewEntries.Add(root);
                            PackViewEntries.OrderBy(p => p.Name);
                        }

                        string[] fileItem = packFileEntry.FullName.Split('\\');
                        if (fileItem.Any())
                        {
                            PackViewerEntry subRoot =
                                root.SubItems.FirstOrDefault(x => x.Name.Equals(fileItem[0]) && x.Level.Equals(2));
                            if (subRoot == null)
                            {
                                subRoot = new PackViewerEntry
                                {
                                    Level = 2,
                                    Name = fileItem[0],
                                };
                                root.SubItems.Add(subRoot);
                                root.SubItems.OrderBy(p => p.Name);
                            }

                            if (fileItem.Length > 1)
                            {
                                PackViewerEntry parentItem = subRoot;
                                int level = 3;
                                for (int i = 1; i < fileItem.Length; ++i)
                                {
                                    PackViewerEntry subItem =
                                        parentItem.SubItems.FirstOrDefault(
                                            x => x.Name.Equals(fileItem[i]) && x.Level.Equals(level));
                                    if (subItem == null)
                                    {
                                        subItem = new PackViewerEntry()
                                        {
                                            Name = fileItem[i],
                                            Level = level,
                                        };
                                        parentItem.SubItems.Add(subItem);
                                        parentItem.SubItems.OrderBy(p => p.Name);
                                    }

                                    parentItem = subItem;
                                    level++;
                                }
                            }
                        }
                    }

                    PackViewTreeView.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    PackViewTreeView.Items.Refresh();
                });
            });
        }

        private async void PackOperationsMergePacksOnClick(object sender, RoutedEventArgs e)
        {
			// Reader must be kept open to pull the data streams
			using (var packReader = new PackReader())
			{
				_pluginContext.SetPatcherState(true);
				PackOperationsLoader.IsOpen = true;
				await Task.Delay(500);

				var packEntryCollection = new List<PackListEntry>();
				var selectedPackViewModels = PackOperationsViewModels.Where(pvm => pvm.IsSequenceTargetable && (pvm.PackVersion >= FromValue && pvm.PackVersion <= ToValue)).OrderBy(pvm => pvm.PackVersion).ToList();

				// This will then only take the overrides between the selected pack files you would like to merge
				foreach (var packViewModel in PackOperationsViewModels.Where(pvm => pvm.IsSequenceTargetable).OrderBy(pvm => pvm.PackName))
				{
					try
					{
						packReader.Load(packViewModel.FilePath);
					}
					catch (Exception ex)
					{
						_pluginContext.LogException(ex, false);
					}
				}

				packEntryCollection = packReader.GetEntries().OrderBy(ple => ple.PackFilePath).ThenBy(ple => ple.FullName).ToList();

				var packagePath = $"package";
				var packFolder = $"{packagePath}\\data";

				// Just in case
				var version = await _pluginContext.GetNexonApi().GetLatestVersion();

				await Task.Run(() =>
				{
					double entries = packEntryCollection.Count;
					int progress = 0;

					long bytes = packEntryCollection.Sum(p => p.DecompressedSize);

					Dispatcher.Invoke(() =>
					{
						ProgressBar.Value = 0;
						ProgressText.Text = $"0 of {entries} ({ByteSizeHelper.ToString(bytes)} left)";
					});

					var packName = "";

					var beginningPack = selectedPackViewModels.FirstOrDefault().PackName;
					var endingPack = selectedPackViewModels.LastOrDefault().PackName;

					if (beginningPack.EndsWith("full.pack", StringComparison.OrdinalIgnoreCase))
					{
						var matchRegex = @"(_\d+)";
						var match = Regex.Match(endingPack, matchRegex).Value;
						match = match.Replace("_", "");

						packName = $"{match}_full.pack";
					}
					else
					{
						var matchRegex = @"(\d+_)";
						var match = Regex.Match(beginningPack, matchRegex).Value;
						var fromMatch = match.Replace("_", "");

						matchRegex = @"(_\d+)";
						match = Regex.Match(endingPack, matchRegex).Value;
						var toMatch = match.Replace("_", "");

						packName = $"{fromMatch}_to_{toMatch}.pack";
					}

					using (var pw = new PackWriter($"{ packagePath}\\{packName}", version))
					{
						foreach (var entry in packEntryCollection)
						{
							var fileStream = entry.GetCompressedDataAsStream();

							pw.WriteDirect(fileStream, entry.FullName, (int)entry.Seed, (int)entry.CompressedSize, (int)entry.DecompressedSize, entry.IsCompressed, entry.CreationTime, entry.LastWriteTime, entry.LastAccessTime);
							fileStream.Dispose();

							progress++;
							bytes -= entry.DecompressedSize;

							Dispatcher.Invoke(() =>
							{
								ProgressBar.Value = (progress / entries) * 100;
								ProgressText.Text = $"{progress} of {entries} ({ByteSizeHelper.ToString(bytes)} left)";
							});
						}

						Dispatcher.Invoke(() =>
						{
							ProgressBar.Value = 0;
							ProgressBar.IsIndeterminate = true;
							ProgressText.Text = $"Creating Pack File...";
						});

						pw.Pack();

						if (PackOpsSettingsManager.Instance.PackOpsSettings.DeletePackFilesAfterMerge)
						{
							Dispatcher.Invoke(() =>
							{
								ProgressBar.Value = 0;
								ProgressBar.IsIndeterminate = true;
								ProgressText.Text = $"Cleaning Up...";
							});

							foreach (var model in selectedPackViewModels)
							{
								try
								{
									File.Delete(model.FilePath);
								}
								catch (Exception ex)
								{
									_pluginContext.LogString($"Unable to delete {model.PackName}", false);
									_pluginContext.LogException(ex, false);
								}
							}
						}
					}
				});
			}

			ProgressBar.Value = 0;
			ProgressBar.IsIndeterminate = true;
			ProgressText.Text = $"Getting Packs...";

			GetPacksForClientProfile();

			PackOperationsLoader.IsOpen = false;

			_pluginContext.SetPatcherState(false);
		}
    }
}