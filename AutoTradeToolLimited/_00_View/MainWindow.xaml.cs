using System;
using System.ComponentModel;
using System.Windows;

using AutoTradeTool._10_ViewModel.MainWindowVM;
using AutoTradeTool._20_Model;
using AutoTradeTool._20_Model.AutoRebalanceM.TopM;
using AutoTradeTool._20_Model.OptionDistortionCancellerM;

namespace AutoTradeTool._00_View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Password.Password = Parameter.Password;
            TradePassword.Password = "";
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ((MainWindowVM)this.DataContext).Password = this.Password.Password;
        }

        private void TradePassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ((MainWindowVM)this.DataContext).TradePassword = this.TradePassword.Password;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AutoRebalanceTopM.Instance.Restore();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (AutoRebalanceTopM.Instance.AutoRebalanceRunning)
            {
                MessageBox.Show(
                    "終了前に自動売買停止してください。",
                    "警告",
                    MessageBoxButton.OK, MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly
                );
                e.Cancel = true;
            }
            else
            {
                AutoRebalanceTopM.Instance.Save();
            }
        }
    }
}
