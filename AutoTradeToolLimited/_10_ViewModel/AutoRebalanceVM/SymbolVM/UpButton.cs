using System;
using System.Windows.Input;

using AutoTradeTool._20_Model.AutoRebalanceM.SymbolM;
using AutoTradeTool._20_Model.AutoRebalanceM.TopM;

namespace AutoTradeTool._10_ViewModel.AutoRebalanceVM.SymbolVM
{
    class UpButton : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private AutoRebalanceSymbolM _RefModel;

        public UpButton(AutoRebalanceSymbolM refModel)
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
            AutoRebalanceTopM.Instance.UpSymbol(_RefModel);
        }
    }
}
