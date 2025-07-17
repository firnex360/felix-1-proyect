using System.Collections.ObjectModel;
using felix1.Logic;
using felix1.Data;
using Microsoft.EntityFrameworkCore;

namespace felix1.OrderSection;

public partial class BalanceVisual : ContentPage
{
	private CashRegister _cashRegister;
	public BalanceVisual(CashRegister cashRegister)
	{
		InitializeComponent();
		_cashRegister = cashRegister;
		lblCashRegisterInfo.Text = $"Caja #{_cashRegister.Number} " +
			$"Abierta por: {_cashRegister.Cashier?.Name ?? "Desconocido"}\n" +
			$"Hora de apertura: {_cashRegister.TimeStarted:dd/MM/yyyy HH:mm}\n" +
			$"Hora de cierre: {(_cashRegister.TimeFinish.HasValue ? _cashRegister.TimeFinish.Value.ToString("dd/MM/yyyy HH:mm") : "No ha cerrado")}\n" +
			$"Total de Ordenes: {GetTotalOrders()}\n" +
			$"Dinero inicial: {_cashRegister.InitialMoney:C}\n" +
			$"   +++ En Efectivo: {GetTotalMoney(1):C}\n" +
			$"   +++ Con Tarjeta: {GetTotalMoney(2):C}\n" +
			$"   +++ Por Transferencia: {GetTotalMoney(3):C}\n" +
			$"   --- Descuentos: {GetTotalRefunds():C}\n" +
			$"[Dinero total en caja: {GetTotalMoney(4):C}]\n" +
			$"{GetOrdersByWaiters()}" +
			$"{GetTaxesByWaiters()} \n";
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
					.Count(i => i.Article.Category == ArticleCategory.Principal)
			})
			.ToList();

		int totalGlobal = groupedByMesero.Sum(g => g.TotalPlatos);
		string resumen = "\n~~~ Platos del Día (Principal) ~~~\n";
		foreach (var meseroGroup in groupedByMesero)
		{
			resumen += $" ~ {meseroGroup.Mesero}: {meseroGroup.TotalPlatos}\n";
		}
		resumen += $"  Total platos del día: {totalGlobal}\n";
		return resumen;

	}
	private string GetTaxesByWaiters()
	{
		var transactions = AppDbContext.ExecuteSafeAsync(async db =>
			await db.Transactions
				.Where(t => t.Order!.CashRegister == _cashRegister)
				.Include(t => t.Order!.Waiter)
				.ToListAsync())
			.GetAwaiter().GetResult();

		decimal totalITBIS = transactions.Sum(t => t.TaxAmountITBIS);

		var meseros = transactions
			.Select(t => t.Order!.Waiter)
			.Where(w => w != null)
			.Distinct()
			.ToList();

		int meseroCount = meseros.Count;

		decimal itbisPerMesero = meseroCount > 0 ? totalITBIS / meseroCount : 0;

		string resumen = "\n~~~ Impuestos por Mesero ~~~\n";

		resumen += $"Total ITBIS del día: {totalITBIS:C} \nDividido entre {meseroCount} mesero(s): {itbisPerMesero:C}\n";

		return resumen;
	}
	
	
    private async void OnGoToLogin(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }


}