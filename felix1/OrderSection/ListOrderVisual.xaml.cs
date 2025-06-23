namespace felix1.OrderSection;

public partial class ListOrderVisual : ContentPage
{
    public static ListOrderVisual? Instance { get; private set; }
	public ListOrderVisual()
	{
		InitializeComponent();
        BindingContext = this;
        Instance = this;
	}

    
}