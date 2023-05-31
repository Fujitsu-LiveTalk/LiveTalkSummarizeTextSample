/*
 * Copyright 2023 FUJITSU LIMITED
 * クラス名　：MainWindow
 * 概要      ：メイン画面
*/
using System.Windows;

namespace LiveTalkSummarizeTextSample.Views
{
    public partial class MainWindow : Window
    {
        public ViewModels.MainViewModel ViewModel { get; } = App.MainVM;

        public MainWindow()
        {
            InitializeComponent();

            this.ViewModel.Closed += (s, args) =>
            {
                this.Close();
            };
        }
    }
}
