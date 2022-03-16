using System.Reflection;
using System.Threading;
using System.Windows;

namespace AutoTradeTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Semaphore _Semaphore;

        protected override void OnStartup(StartupEventArgs e)
        {
            string semaphoreName = Assembly.GetExecutingAssembly().GetName().Name;
            bool createdNew;

            // Semaphoreクラスのインスタンスを生成し、アプリケーション終了まで保持する
            _Semaphore = new Semaphore(1, 1, semaphoreName, out createdNew);
            if (!createdNew)
            {
                // 他のプロセスが先にセマフォを作っていた
                MessageBox.Show("すでに起動しています", semaphoreName, MessageBoxButton.OK, MessageBoxImage.Hand);
                Shutdown();
            }
        }
        protected override void OnExit(ExitEventArgs e)
        {
            _Semaphore.Dispose();
        }
    }
}
