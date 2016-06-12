﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="ProgressService.cs">
//   Copyright 2014 - Present Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ChocolateyGui.Base;
using ChocolateyGui.Controls;
using ChocolateyGui.Controls.Dialogs;
using ChocolateyGui.Models;
using ChocolateyGui.Utilities;
using ChocolateyGui.Views;
using MahApps.Metro.Controls.Dialogs;

namespace ChocolateyGui.Services
{
    public class ProgressService : ObservableBase, IProgressService
    {
        private readonly AsyncLock _lock;
        private CancellationTokenSource _cst;
        private int _loadingItems;
        private ChocolateyDialogController _progressController;

        public ProgressService()
        {
            IsLoading = false;
            _loadingItems = 0;
            Output = new ObservableRingBufferCollection<PowerShellOutputLine>(100);
            _lock = new AsyncLock();
        }

        public ShellView ShellView { get; set; }

        public double Progress { get; private set; }

        public bool IsLoading { get; private set; }

        public ObservableRingBufferCollection<PowerShellOutputLine> Output { get; }

        public CancellationToken GetCancellationToken()
        {
            if (!IsLoading)
            {
                throw new InvalidOperationException("There's no current operation in process.");
            }

            return _cst.Token;
        }

        public void Report(double value)
        {
            Progress = value;

            if (_progressController != null)
            {
                if (value < 0)
                {
                    _progressController.SetIndeterminate();
                }
                else
                {
                    _progressController.SetProgress(Math.Min(Progress / 100.0f, 100));
                }
            }

            NotifyPropertyChanged("Progress");
        }

        public async Task<MessageDialogResult> ShowMessageAsync(string title, string message)
        {
            if (ShellView != null)
            {
                return await ShellView.ShowMessageAsync(title, message);
            }

            return MessageBox.Show(message, title) == MessageBoxResult.OK
                ? MessageDialogResult.Affirmative
                : MessageDialogResult.Negative;
        }

        public async Task StartLoading(string title = null, bool isCancelable = false)
        {
            using (await _lock.LockAsync())
            {
                var currentCount = Interlocked.Increment(ref _loadingItems);
                if (currentCount == 1)
                {
                    _progressController = await ShellView.ShowChocolateyDialogAsync(title, isCancelable);
                    _progressController.SetIndeterminate();
                    if (isCancelable)
                    {
                        _cst = new CancellationTokenSource();
                        _progressController.OnCanceled += dialog =>
                        {
                            if (_cst != null)
                            {
                                _cst.Cancel();
                            }
                        };
                    }

                    Output.Clear();

                    IsLoading = true;
                    NotifyPropertyChanged("IsLoading");
                }
            }
        }

        public async Task StopLoading()
        {
            using (await _lock.LockAsync())
            {
                var currentCount = Interlocked.Decrement(ref _loadingItems);
                if (currentCount == 0)
                {
                    await _progressController.CloseAsync();
                    _progressController = null;
                    Report(0);

                    IsLoading = false;
                    NotifyPropertyChanged("IsLoading");
                }
            }
        }

        public void WriteMessage(string message, PowerShellLineType type = PowerShellLineType.Output,
            bool newLine = true)
        {
            Output.Add(new PowerShellOutputLine(message, type, newLine));
        }
    }
}