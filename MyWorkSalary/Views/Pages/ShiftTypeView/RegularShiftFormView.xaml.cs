using MyWorkSalary.ViewModels.ShiftTypes;

namespace MyWorkSalary.Views.Pages.ShiftTypeView;

public partial class RegularShiftFormView : ContentView
{
    public RegularShiftFormView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is RegularShiftViewModel vm)
        {
            vm.Reset();
            vm.RefreshPremiumState();  
        }
    }
}
