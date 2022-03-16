using System;
using System.Windows;
using System.Windows.Input;

using AutoTradeTool._20_Model;
using AutoTradeTool._20_Model.AutoRebalanceM.TopM;
using AutoTradeTool._20_Model.MainWindowM;

namespace AutoTradeTool._10_ViewModel.AutoRebalanceVM.TopVM
{
    class UpdateButton : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                AutoRebalanceTopM.Instance.GetSymbolInfo();
                AutoRebalanceTopM.Instance.SyncPositionInfo();
                AutoRebalanceTopM.Instance.GetBoardInfo();

                Mouse.OverrideCursor = null;
                MessageBox.Show("OK");
            }
            catch (Exception ex)
            {
                Communication.ClearToken();
                Communication.StopWebSocket();
                MainWindowM.Instance.IsConnected = false;
                Mouse.OverrideCursor = null;
                MessageBox.Show(ex.Message);
            }
        }
    }
}
