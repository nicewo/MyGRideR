using MYG.RideR.Mobile.ViewModels;

namespace MYG.RideR.Mobile.Views;

public partial class TripsPage : ContentPage
{
    public TripsPage(TripsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
