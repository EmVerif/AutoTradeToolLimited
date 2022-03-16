using System;
using System.Windows.Input;

using AutoTradeTool._20_Model.AutoRebalanceM.SymbolM;
using AutoTradeTool._20_Model.AutoRebalanceM.TopM;

namespace AutoTradeTool._10_ViewModel.AutoRebalanceVM.SymbolVM
{
    class DownButton : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private AutoRebalanceSymbolM _RefModel;

        public DownButton(AutoRebalanceSymbolM refModel)
        {
            _RefModel = refModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
            AutoRebalanceTopM.Instance.DownSymbol(_RefModel);
        }
    }
}
