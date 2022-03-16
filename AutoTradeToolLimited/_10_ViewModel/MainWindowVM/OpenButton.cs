using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using AutoTradeTool._20_Model;
using AutoTradeTool._20_Model.MainWindowM;

namespace AutoTradeTool._10_ViewModel.MainWindowVM
{
    class OpenButton : ICommand
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
                Communication.UnregisterAll();
                Communication.StartWebSocket();
                MainWindowM.Instance.IsConnected = true;
            }
            catch (Exception ex)
            {
                Communication.ClearToken();
                Communication.StopWebSocket();
                MainWindowM.Instance.IsConnected = false;
                MessageBox.Show(ex.Message);
            }
        }
    }
}
