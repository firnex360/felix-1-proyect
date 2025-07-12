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
			$"Total de Ordenes: {GetTotalOrders()}\n" +
			$"Dinero inicial: {_cashRegister.InitialMoney:C}\n" +
			$" +++ En Efectivo: {GetTotalMoney(1):C}\n" +
			$" +++ Con Tarjeta: {GetTotalMoney(2):C}\n" +
			$" +++ Por Transferencia: {GetTotalMoney(3):C}\n" +
			$" --- Descuentos: {GetTotalRefunds():C}\n" +
			$"Dinero total: {GetTotalMoney(4):C}\n" +
			//$"Ordenes por Mesero: {GetOrdersByWaiters()} \n" +
			$"Hora de cierre: {(_cashRegister.TimeFinish.HasValue ? _cashRegister.TimeFinish.Value.ToString("dd/MM/yyyy HH:mm") : "No ha cerrado")}\n";
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

	private void GetOrdersByWaiters()
	{
		/*var orderItems = AppDbContext.ExecuteSafeAsync(async db =>
			await db.OrderItems
				.Include(oi => oi.Article)
				.Include(oi => oi.Order)
					.ThenInclude(o => o.Mesero)
				.Where(oi => oi.Order.CashRegister == _cashRegister && oi.Article.Category == ArticleCategory.Principal)
				.ToListAsync())
			.GetAwaiter().GetResult();

		var groupedByMesero = orderItems
			.GroupBy(oi => oi.Order.Mesero)
			.Select(group => new
			{
				Mesero = group.Key?.Name ?? "Desconocido",
				TotalPlatos = group.Sum(oi => oi.Quantity)
			})
			.ToList();

		int totalGlobal = groupedByMesero.Sum(g => g.TotalPlatos);

		string resumen = "\n--- Platos del Día (Principal) ---\n";
		foreach (var meseroGroup in groupedByMesero)
		{
			resumen += $"  {meseroGroup.Mesero}: {meseroGroup.TotalPlatos}\n";
		}

		resumen += $"  Total platos del día: {totalGlobal}\n";
		return resumen;*/
	} 

}