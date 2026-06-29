using MYG.RideR.Mobile.ViewModels;

namespace MYG.RideR.Mobile.Views;

public partial class StatisticsPage : ContentPage
{
    public StatisticsPage(StatisticsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
