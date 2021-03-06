﻿using System.Windows;
using System.Windows.Controls;
using HyddwnLauncher.Core;
using Microsoft.Win32;

namespace HyddwnLauncher.Controls
{
    /// <summary>
    ///     Interaction logic for NewClientProfileUserControl.xaml
    /// </summary>
    public partial class NewClientProfileUserControl : UserControl
    {
        public static readonly DependencyProperty ClientProfileProperty = DependencyProperty.Register(
            "ClientProfile", typeof(ClientProfile), typeof(NewClientProfileUserControl),
            new PropertyMetadata(default(ClientProfile)));

        public static readonly DependencyProperty CredentialUsernameProperty = DependencyProperty.Register(
            "CredentialUsername", typeof(string), typeof(NewClientProfileUserControl), new PropertyMetadata(default(string)));


        public NewClientProfileUserControl()
        {
            InitializeComponent();
        }

        public ClientProfile ClientProfile
        {
            get => (ClientProfile) GetValue(ClientProfileProperty);
            set => SetValue(ClientProfileProperty, value);
        }

        public string CredentialUsername
        {
            get => (string)GetValue(CredentialUsernameProperty);
            set => SetValue(CredentialUsernameProperty, value);
        }

        private void BrowseButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (ClientProfile == null)
            {
                ErrorWindow.IsOpen = true;
                return;
            }

            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executables (*.exe)|*.exe";
            openFileDialog.InitialDirectory = "C:\\Nexon\\Library\\mabinogi\\appdata\\";
            if (openFileDialog.ShowDialog() == true)
                ClientProfile.Location = openFileDialog.FileName;
        }

        private void ClientProfileSavedCredentialsRemoveButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (ClientProfile == null) return;

            CredentialsStorage.Instance.Remove(ClientProfile.Guid);
            CredentialUsername = "";
        }
    }
}