namespace MyWorkSalary.Helpers
{
    public static class NavigationHelper
    {
        /// <summary>
        /// Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
        /// blir tillfälligt mörk/grå efter att man tryckt på back-pilen.
        ///
        /// Lösningen ersätter standard back-navigation med en egen back-command
        /// som navigerar utan animation (animated = false).
        ///
        /// Används i OnAppearing() på sidor som navigeras via Shell.
        /// </summary>
        public static void UseNoAnimationBackButton(ContentPage page)
        {
            Shell.SetBackButtonBehavior(page, new BackButtonBehavior
            {
                Command = new Command(async () =>
                {
                    await Shell.Current.GoToAsync("..", false);
                })
            });
        }
    }
}
