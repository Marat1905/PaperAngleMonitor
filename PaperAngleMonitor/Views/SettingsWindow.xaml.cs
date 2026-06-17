using PaperAngleMonitor.ViewModels;
using System.Windows;

namespace PaperAngleMonitor.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            _viewModel.SetWindow(this);
            Loaded += (s, e) => _viewModel.LoadSettings();
        }
    }
}