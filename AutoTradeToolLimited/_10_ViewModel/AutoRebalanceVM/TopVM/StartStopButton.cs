using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using AutoTradeTool._20_Model;
using AutoTradeTool._20_Model.AutoRebalanceM.TopM;
using AutoTradeTool._20_Model.MainWindowM;

namespace AutoTradeTool._10_ViewModel.AutoRebalanceVM.TopVM
{
    class StartStopButton : ICommand, INotifyPropertyChanged
    {
        public event EventHandler CanExecuteChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string ButtonName { get; private set; } = "トレード開始";

        private Boolean _IsBusy = false;

        public bool CanExecute(object parameter)
        {
            return !_IsBusy;
        }

        public void Execute(object parameter)
        {
            _IsBusy = true;
            CanExecuteChanged?.Invoke(this, new EventArgs());
            try
            {
                if (!AutoRebalanceTopM.Instance.AutoRebalanceRunning)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            _IsBusy = false;
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        private void Stop()
        {
            var result = MessageBox.Show(
                "停止して良いですか？",
                "質問",
                MessageBoxButton.OKCancel
            );
            if (result == MessageBoxResult.OK)
            {
                AutoRebalanceTopM.Instance.StopAutoRebalance();
            }
        }

        private async void Start()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var list = AutoRebalanceTopM.Instance.Database.Select(x => x.Symbol).ToList();
                Communication.RegisterTosho(list);
                AutoRebalanceTopM.Instance.CheckAutoRebalance();
                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                Communication.ClearToken();
                Communication.StopWebSocket();
                MainWindowM.Instance.IsConnected = false;
                Mouse.OverrideCursor = null;
                MessageBox.Show(ex.Message, "停止", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

                return;
            }
            var result = MessageBox.Show(
                "開始して良いですか？",
                "確認",
                MessageBoxButton.OKCancel
            );
            if (result == MessageBoxResult.OK)
            {
                ButtonName = "トレード停止";
                OnPropertyChanged(nameof(this.ButtonName));
                try
                {
                    await AutoRebalanceTopM.Instance.StartAutoRebalanceAsync();
                }
                catch (Exception ex)
                {
                    AutoRebalanceTopM.Instance.StopAutoRebalance();
                    Communication.ClearToken();
                    Communication.StopWebSocket();
                    MainWindowM.Instance.IsConnected = false;
                    MessageBox.Show(ex.Message, "停止", MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
                finally
                {
                    AutoRebalanceTopM.Instance.Save();
                    ButtonName = "トレード開始";
                    OnPropertyChanged(nameof(this.ButtonName));
                }
            }
        }

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}

