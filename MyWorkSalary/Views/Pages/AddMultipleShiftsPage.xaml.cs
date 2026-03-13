using Microsoft.Maui.Controls;
using MyWorkSalary.Helpers;
using MyWorkSalary.Helpers.Calendar;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace MyWorkSalary.Views.Pages
{
    public partial class AddMultipleShiftsPage : ContentPage
    {
        private readonly AdService _adService;
        public AddMultipleShiftsPage(AddMultipleShiftsViewModel viewModel, AdService adService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _adService = adService;

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
            NavigationHelper.UseNoAnimationBackButton(this);

            // Visa ad-banner!
            _adService.ShowBanner();
        }
    }
}