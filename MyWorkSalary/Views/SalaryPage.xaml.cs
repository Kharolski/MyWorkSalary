using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Views
{
    public partial class SalaryPage : ContentPage
    {
        public SalaryPage(SalaryPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is SalaryPageViewModel vm)
            {
                vm.ResetToCurrentMonth(); 
                await vm.LoadData(); // Hämtar ActiveJob + beräknar CurrentStats
            }
        }

        private async Task SlideMonthAsync(int direction)
        {
            // direction: -1 = föregående (slide åt höger), +1 = nästa (slide åt vänster)
            // Vi vill att innehållet rör sig i samma riktning som man "bläddrar"
            // Nästa månad: innehållet slidear åt vänster
            // Föregående månad: innehållet slidear åt höger

            const uint outDuration = 110;
            const uint inDuration = 140;
            const double distance = 40;

            // slide ut + fade
            await Task.WhenAll(
                MonthCardFrame.TranslateTo(-direction * distance, 0, outDuration, Easing.CubicIn),
                MonthCardFrame.FadeTo(0.2, outDuration, Easing.CubicIn)
            );

            // byt data (VM uppdaterar bindings)
            // (inget här – vi gör det i eventet innan vi animerar in)

            // hoppa till andra sidan (för "in")
            MonthCardFrame.TranslationX = direction * distance;

            // slide in + fade
            await Task.WhenAll(
                MonthCardFrame.TranslateTo(0, 0, inDuration, Easing.CubicOut),
                MonthCardFrame.FadeTo(1.0, inDuration, Easing.CubicOut)
            );
        }

        private async void PrevMonthTapped(object sender, EventArgs e)
        {
            if (BindingContext is not SalaryPageViewModel vm)
                return;
            if (!vm.CanGoPrevMonth)
                return;

            // Uppdatera först månad
            vm.PrevMonthCommand.Execute(null);

            // direction -1 = föregående (slide åt höger)
            await SlideMonthAsync(direction: -1);
        }

        private async void NextMonthTapped(object sender, EventArgs e)
        {
            if (BindingContext is not SalaryPageViewModel vm)
                return;
            if (!vm.CanGoNextMonth)
                return;

            vm.NextMonthCommand.Execute(null);

            // direction +1 = nästa (slide åt vänster)
            await SlideMonthAsync(direction: +1);
        }

        private async void OnSwipedLeft(object sender, SwipedEventArgs e)
        {
            // swipe vänster = nästa månad
            NextMonthTapped(sender, EventArgs.Empty);
        }

        private async void OnSwipedRight(object sender, SwipedEventArgs e)
        {
            // swipe höger = föregående månad
            PrevMonthTapped(sender, EventArgs.Empty);
        }

    }
}
