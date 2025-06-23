using felix1.Logic;

namespace felix1.OrderSection;

public partial class CreateTableVisual : ContentPage
{
	public CreateTableVisual()
	{
		InitializeComponent();
	}

	private void OnCreateClicked(object sender, EventArgs e)
	{
		var table = new Table
		{
			LocalNumber = int.TryParse(entryLocalNumber.Text, out var local) ? local : 0,
			GlobalNumber = int.TryParse(entryGlobalNumber.Text, out var global) ? global : 0,
			IsTakeOut = false,
			IsBillRequested = false,
			IsPaid = false
		};
	}
	


}