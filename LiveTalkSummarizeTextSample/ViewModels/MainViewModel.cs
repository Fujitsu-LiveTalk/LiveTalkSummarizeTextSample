/*
 * Copyright 2023 FUJITSU LIMITED
 * クラス名　：MainViewModel
 * 概要      ：MainViewModel
*/

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;

namespace LiveTalkSummarizeTextSample.ViewModels
{
    public class MainViewModel : IDisposable
    {
        private Models.SummarizeModel Model = new Models.SummarizeModel();
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();

        #region "Property"
        [Required]       // 必須チェック
        public ReactiveProperty<string> FileName { get; }
        [Required, Range(1, 100)]       // 必須チェック
        public ReactiveProperty<int> Ratio { get; }
        public ReactiveProperty<string> Message { get; }
        public ReactiveProperty<string> Result { get; }
        public ReactiveProperty<bool> IsBusy { get; } = new ReactiveProperty<bool>(false);
        #endregion

        /// <summary>
        /// True:連携開始可能
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsCanStart { get; }

        /// <summary>
        /// True:連携中フラグ
        /// </summary>
        public ReactiveProperty<bool> IsStarted { get; } = new ReactiveProperty<bool>();

        public MainViewModel()
        {
            // プロパティ設定
            this.FileName = this.Model.ToReactivePropertyAsSynchronized((x) => x.FileName)
                .SetValidateAttribute(() => this.FileName)
                .AddTo(this.Disposable);
            this.Ratio = this.Model.ToReactivePropertyAsSynchronized((x) => x.Ratio)
                .SetValidateAttribute(() => this.Ratio)
                .AddTo(this.Disposable);
            this.Message = this.Model.ToReactivePropertyAsSynchronized((x) => x.Message)
                .AddTo(this.Disposable);
            this.Result = this.Model.ToReactivePropertyAsSynchronized((x) => x.Result)
                .AddTo(this.Disposable);

            // 3つのステータスがすべてFalseの時だけスタートボタンがクリックできる
            this.IsCanStart = new[]
            {
                this.FileName.ObserveHasErrors,
                this.Ratio.ObserveHasErrors,
                this.IsStarted,
            }.CombineLatestValuesAreAllFalse()
             .ToReadOnlyReactiveProperty()
             .AddTo(this.Disposable);

            // コマンド設定
            this.FileOpenCommand = this.IsStarted.Inverse()
                .ToReactiveCommand()
                .WithSubscribe(() => this.FileInput())
                .AddTo(this.Disposable);
            this.StartCommand = this.IsCanStart
                .ToReactiveCommand()
                .WithSubscribe(async () => await this.Start())
                .AddTo(this.Disposable);
            this.ExitCommand.Subscribe((x) =>
            {
                OnClosed();
            }).AddTo(this.Disposable);

            // エラーハンドリング
            this.Model.Threw += (s, e) =>
            {
                MessageBox.Show(e.GetException().Message, "LiveTalk SummarizeText Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
            };

        }

        /// <summary>
        /// ファイル入力
        /// </summary>
        public ReactiveCommand FileOpenCommand { get; }
        private void FileInput()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FilterIndex = 1,
                    Filter = "LiveTalk CSVファイル(*.csv)|*.csv",
                    Title = "ファイル名を指定",
                    CreatePrompt = true,
                    OverwritePrompt = false,
                    DefaultExt = "csv"
                };
                if (string.IsNullOrEmpty(this.FileName.Value))
                {
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    dialog.FileName = "Output.csv";
                }
                else
                {
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(this.FileName.Value);
                    dialog.FileName = System.IO.Path.GetFileName(this.FileName.Value);
                }
                if (dialog.ShowDialog() == true)
                {
                    this.FileName.Value = dialog.FileName;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 要約
        /// </summary>
        public ReactiveCommand StartCommand { get; } = new ReactiveCommand();
        private async Task Start()
        {
            try
            {
                this.IsBusy.Value = true;
                this.IsStarted.Value = true;
                await this.Model.Convert();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.IsStarted.Value = false;
                this.IsBusy.Value = false;
            }
        }

        /// <summary>
        /// 画面クローズ
        /// </summary>
        public ReactiveCommand ExitCommand { get; } = new ReactiveCommand();
        public event EventHandler Closed;
        protected virtual void OnClosed()
        {
            this.Closed?.Invoke(this, new EventArgs());
        }

        public void Dispose()
        {
            this.Disposable.Dispose();
        }
    }
}
