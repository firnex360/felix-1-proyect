using System.Collections.ObjectModel;
using felix1.Logic;
using felix1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage; // For Preferences

#if WINDOWS
using System.Drawing;
using System.Drawing.Printing;
#endif

namespace felix1.OrderSection;

public partial class BalanceVisual : ContentPage
{
	private CashRegister _cashRegister;
	
	public BalanceVisual(CashRegister cashRegister)
	{
		InitializeComponent();
		_cashRegister = cashRegister;
		LoadBalanceData();
	}

	private void LoadBalanceData()
	{
		// Header information
		lblCashRegisterHeader.Text = $"Caja #{_cashRegister.Number} • Cajero: {_cashRegister.Cashier?.Name ?? "Desconocido"}\n" +
			$"Abierta: {_cashRegister.TimeStarted:dd/MM/yyyy HH:mm} • " +
			$"Cerrada: {(_cashRegister.TimeFinish.HasValue ? _cashRegister.TimeFinish.Value.ToString("dd/MM/yyyy HH:mm") : "Activa")}";

		// Financial summary
		lblInitialMoney.Text = _cashRegister.InitialMoney.ToString("C");
		lblTotalOrders.Text = GetTotalOrders().ToString();
		lblTotalMoney.Text = GetTotalMoney(4).ToString("C");

		// Payment methods
		lblCashAmount.Text = GetTotalMoney(1).ToString("C");
		lblCardAmount.Text = GetTotalMoney(2).ToString("C");
		lblTransferAmount.Text = GetTotalMoney(3).ToString("C");
		lblRefundsAmount.Text = GetTotalRefunds().ToString("C");

		// Waiter statistics
		lblWaiterStats.Text = GetOrdersByWaiters();

		// Discount information
		lblDiscountInfo.Text = GetDiscountInfo();

		// Tax information
		lblTaxInfo.Text = GetTaxesByWaiters();
	}

	private int GetTotalOrders()
	{
		//filter the orders by the current cash register
		var orders = AppDbContext.ExecuteSafeAsync(async db =>
			await db.Orders
				.Where(o => o.CashRegister == _cashRegister)
				.ToListAsync())
			.GetAwaiter().GetResult();

		return orders.Count;
	}

	private decimal GetTotalMoney(int selected)
	{
		//filter the transactions by the current cash register
		var transactions = AppDbContext.ExecuteSafeAsync(async db =>
			await db.Transactions
				.Where(t => t.Order!.CashRegister == _cashRegister)
				.ToListAsync())
			.GetAwaiter().GetResult();

		decimal total = 0;
		if (selected == 1) // Cash
		{
			foreach (var item in transactions)
			{
				total += item.CashAmount;
			}
		}
		else if (selected == 2) // Card
		{
			foreach (var item in transactions)
			{
				total += item.CardAmount;
			}
		}
		else if (selected == 3) // Transfer
		{
			foreach (var item in transactions)
			{
				total += item.TransferAmount;
			}
		}
		else if (selected == 4) // Total
		{
			foreach (var item in transactions)
			{
				total += item.TotalAmount;
			}
			foreach (var item in transactions)
			{
				total -= item.Refund?.RefundedItems?.Sum(ri => ri.TotalPrice) ?? 0;
			}
			total += (decimal)_cashRegister.InitialMoney;
		}
		return total;
	}

	private decimal GetTotalRefunds()
	{
		var transactions = AppDbContext.ExecuteSafeAsync(async db =>
			await db.Transactions
				.Where(t => t.Order!.CashRegister == _cashRegister)
				.Include(t => t.Refund)
				.ThenInclude(r => r!.RefundedItems)
				.ToListAsync())
			.GetAwaiter().GetResult();

		decimal total = 0;
		foreach (var item in transactions)
		{
			total += item.Refund?.RefundedItems?.Sum(ri => ri.TotalPrice) ?? 0;
		}
		return total;
	}

	private string GetOrdersByWaiters()
	{
		var orderItems = AppDbContext.ExecuteSafeAsync(async db =>
			await db.Orders
				.Where(o => o.CashRegister == _cashRegister)
				.Include(o => o.Items!)
					.ThenInclude(i => i.Article)
				.Include(o => o.Waiter)
				.ToListAsync())
			.GetAwaiter().GetResult();

		var groupedByMesero = orderItems
			.GroupBy(o => o.Waiter)
			.Select(group => new
			{
				Mesero = group.Key?.Name ?? "Takeout",
				TotalPlatos = group
					.SelectMany(o => o.Items!)
					.Where(i => i.Article!.Category == ArticleCategory.Principal)
					.Sum(i => i.Quantity), // Sum the quantities instead of counting items
				TotalOrdenes = group.Count()
			})
			.OrderByDescending(g => g.TotalPlatos)
			.ToList(); 

		int totalGlobal = groupedByMesero.Sum(g => g.TotalPlatos);
		int totalOrdenesGlobal = groupedByMesero.Sum(g => g.TotalOrdenes);
		
		string resumen = $"Total de platos principales: {totalGlobal}\n";
		resumen += $"Total de órdenes procesadas: {totalOrdenesGlobal}\n\n";
		resumen += "Desglose por mesero:\n";

		foreach (var meseroGroup in groupedByMesero)
		{
			resumen += $"• {meseroGroup.Mesero}: {meseroGroup.TotalPlatos} platos ({meseroGroup.TotalOrdenes} órdenes)\n";
		}
		
		return resumen;
	}

	private string GetDiscountInfo()
	{
		var orders = AppDbContext.ExecuteSafeAsync(async db =>
			await db.Orders
				.Where(o => o.CashRegister == _cashRegister)
				.Include(o => o.Waiter)
				.ToListAsync())
			.GetAwaiter().GetResult();

		decimal totalDiscounts = orders.Sum(o => o.Discount);
		int ordersWithDiscounts = orders.Count(o => o.Discount > 0);
		int totalOrders = orders.Count;

		var discountsByWaiter = orders
			.Where(o => o.Discount > 0)
			.GroupBy(o => o.Waiter)
			.Select(group => new
			{
				Mesero = group.Key?.Name ?? "Takeout",
				TotalDescuentos = group.Sum(o => o.Discount),
				CantidadOrdenes = group.Count()
			})
			.OrderByDescending(g => g.TotalDescuentos)
			.ToList();

		string resultado = $"Total en descuentos: {totalDiscounts:C}\n";
		resultado += $"Órdenes con descuento: {ordersWithDiscounts} de {totalOrders}\n";

		if (discountsByWaiter.Any())
		{
			resultado += "\nDescuentos por mesero:\n";
			foreach (var item in discountsByWaiter)
			{
				resultado += $"• {item.Mesero}: {item.TotalDescuentos:C} ({item.CantidadOrdenes} órdenes)\n";
			}
		}
		else
		{
			resultado += "\nNo se aplicaron descuentos en esta sesión.";
		}

		return resultado;
	}
	
	private string GetTaxesByWaiters()
	{
		var transactions = AppDbContext.ExecuteSafeAsync(async db =>
			await db.Transactions
				.Where(t => t.Order!.CashRegister == _cashRegister)
				.Include(t => t.Order!.Waiter)
				.Include(t => t.Order!.Table)
				.ToListAsync())
			.GetAwaiter().GetResult();

		decimal totalITBIS = transactions.Sum(t => t.TaxAmountITBIS);
		decimal totalWaiterTax = transactions.Where(t => t.Order!.Table!.IsTakeOut == false).Sum(t => t.TaxAmountWaiters);
		decimal totalDeliveryTax = transactions.Where(t => t.Order!.Table!.IsTakeOut == true).Sum(t => t.TaxAmountWaiters);

		var meseros = transactions
			.Select(t => t.Order!.Waiter)
			.Where(w => w != null)
			.Distinct()
			.ToList();

		int meseroCount = meseros.Count;
		decimal itbisPerMesero = meseroCount > 0 ? totalWaiterTax / meseroCount : 0;

		string resumen = $"ITBIS total recaudado: {totalITBIS:C}\n";
		resumen += $"Propinas de meseros: {totalWaiterTax:C}\n";
		resumen += $"Impuesto delivery: {totalDeliveryTax:C}\n\n";
		resumen += $"ITBIS dividido entre {meseroCount} mesero(s):\n {itbisPerMesero:C} c/u";

		return resumen;
	}

	private async void OnPrintBalance(object sender, EventArgs e)
	{
#if WINDOWS
		try
		{
			string balanceText = GenerateBalanceReport();
			
			PrintDocument pd = new PrintDocument();
			var savedPrinter = Preferences.Get("SelectedPrinter", "");
			if (!string.IsNullOrEmpty(savedPrinter))
			{
				pd.PrinterSettings.PrinterName = savedPrinter;
			}
			
			pd.PrintPage += (sender, e) =>
			{
				System.Drawing.Font font = new System.Drawing.Font("Consolas", 9);
				System.Drawing.Font titleFont = new System.Drawing.Font("Consolas", 12, System.Drawing.FontStyle.Bold);
				var lines = balanceText.Split('\n');
				float yPos = 0;
				float lineHeight = 0;
				
				if (e.Graphics != null)
				{
					lineHeight = font.GetHeight(e.Graphics);

					foreach (string line in lines)
					{
						System.Drawing.Font currentFont = line.Contains("BALANCE DE CAJA") || line.Contains("TOTAL") ? titleFont : font;
						e.Graphics.DrawString(line.TrimEnd(), currentFont, System.Drawing.Brushes.Black, new System.Drawing.PointF(0, yPos));
						yPos += lineHeight;
					}
				}
			};

			pd.Print();
			await DisplayAlert("Éxito", "Balance impreso correctamente", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"No se pudo imprimir: {ex.Message}", "OK");
		}
#else
		await DisplayAlert("Info", "La impresión solo está disponible en Windows", "OK");
#endif
	}

	private string GenerateBalanceReport()
	{
		string report = "================================\n";
		report += "     BALANCE DE CAJA\n";
		report += "================================\n\n";
		report += $"Caja: #{_cashRegister.Number}\n";
		report += $"Cajero: {_cashRegister.Cashier?.Name ?? "Desconocido"}\n";
		report += $"Apertura: {_cashRegister.TimeStarted:dd/MM/yyyy HH:mm}\n";
		report += $"Cierre: {(_cashRegister.TimeFinish?.ToString("dd/MM/yyyy HH:mm") ?? "Activa")}\n\n";
		
		report += "RESUMEN FINANCIERO:\n";
		report += "--------------------------------\n";
		report += $"Dinero inicial:      {_cashRegister.InitialMoney:C}\n";
		report += $"Efectivo:            {GetTotalMoney(1):C}\n";
		report += $"Tarjeta:             {GetTotalMoney(2):C}\n";
		report += $"Transferencias:      {GetTotalMoney(3):C}\n";
		report += $"Reembolsos:         -{GetTotalRefunds():C}\n";
		report += $"TOTAL EN CAJA:   {GetTotalMoney(4):C}\n\n";
		
		report += $"Total órdenes:     {GetTotalOrders()}\n\n";
		
		report += GetOrdersByWaiters().Replace("🍽️", "").Replace("📋", "").Replace("📊", "").Replace("•", "-") + "\n";
		report += GetDiscountInfo().Replace("💸", "").Replace("•", "-") + "\n";
		report += GetTaxesByWaiters().Replace("💰", "").Replace("🍽️", "").Replace("🚗", "").Replace("📊", "") + "\n";
		
		report += "================================\n";
		report += $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}\n";
		
		return report;
	}
	
	
    private async void OnGoToLogin(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }


}