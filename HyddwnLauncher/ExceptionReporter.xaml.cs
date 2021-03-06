﻿using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace HyddwnLauncher
{
    /// <summary>
    ///     Interaction logic for ExceptionReporter.xaml
    /// </summary>
    public partial class ExceptionReporter
    {
        private readonly Exception _ex;

        public ExceptionReporter(Exception ex)
        {
            _ex = ex;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ExceptionBox.Text = $"Date: {DateTime.Now}\r\n" +
                                $"OS: {Environment.OSVersion}\r\n" +
                                $"Application Directory: {AppDomain.CurrentDomain.BaseDirectory}\r\n" +
                                $"Current Directory: {Environment.CurrentDirectory}\r\n" +
                                $"System Folder: {Environment.SystemDirectory}\r\n" +
                                $".NET Runtime: {RuntimeEnvironment.GetSystemVersion()}\r\n" +
                                $"Exception Details: {_ex}";
        }
    }
}