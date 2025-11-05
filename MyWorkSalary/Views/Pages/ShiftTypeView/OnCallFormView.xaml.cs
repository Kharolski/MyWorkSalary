namespace MyWorkSalary.Views.Pages.ShiftTypeView;

public partial class OnCallFormView : ContentView
{
    public OnCallFormView()
    {
        InitializeComponent();
        this.Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, EventArgs e)
    {
        if (BindingContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
        this.Unloaded -= OnUnloaded;
    }
}
