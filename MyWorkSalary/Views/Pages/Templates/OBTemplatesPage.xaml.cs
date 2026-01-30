using MyWorkSalary.Helpers;
using MyWorkSalary.ViewModels.Templates;

namespace MyWorkSalary.Views.Pages.Templates;

[QueryProperty(nameof(JobId), "jobId")]
[QueryProperty(nameof(Mode), "mode")]
public partial class OBTemplatesPage : ContentPage
{
    public string JobId { get; set; } = "";
    public string Mode { get; set; } = "add";

    public OBTemplatesPage(OBTemplatesViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is OBTemplatesViewModel vm)
        {
            vm.Initialize(
                int.Parse(JobId),
                Mode == "replace");
        }

        // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
        NavigationHelper.UseNoAnimationBackButton(this);
    }

}