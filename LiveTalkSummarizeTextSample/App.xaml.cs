using System.Windows;

namespace LiveTalkSummarizeTextSample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static ViewModels.MainViewModel _MainVM = null;
        public static ViewModels.MainViewModel MainVM
        {
            get
            {
                if (_MainVM == null)
                {
                    _MainVM = new ViewModels.MainViewModel();
                }
                return _MainVM;
            }
        }
    }
}
