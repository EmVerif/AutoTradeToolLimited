using System;
using System.ComponentModel;
using System.Windows.Input;

using AutoTradeTool._20_Model.AutoRebalanceM.TopM;

namespace AutoTradeTool._10_ViewModel.AutoRebalanceVM.TopVM
{
    class AddButton : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
            AutoRebalanceTopM.Instance.AddSymbol();
        }
    }
}
