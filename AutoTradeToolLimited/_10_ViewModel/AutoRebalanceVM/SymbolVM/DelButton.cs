using System;
using System.Windows;
using System.Windows.Input;

using AutoTradeTool._20_Model.AutoRebalanceM.SymbolM;
using AutoTradeTool._20_Model.AutoRebalanceM.TopM;

namespace AutoTradeTool._10_ViewModel.AutoRebalanceVM.SymbolVM
{
    class DelButton : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private AutoRebalanceSymbolM _RefModel;

        public DelButton(AutoRebalanceSymbolM refModel)
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
            MessageBoxResult result = MessageBoxResult.OK;

            if (_RefModel.Position > 0)
            {
                result = MessageBox.Show(
                    "ポジションありますが削除しますか？",
                    "質問",
                    MessageBoxButton.OKCancel
                );
            }
            if (result == MessageBoxResult.OK)
            {
                AutoRebalanceTopM.Instance.DelSymbol(_RefModel);
            }
        }
    }
}
