using Dapper;
using Npgsql;
//chatgpt helped me with the setup + "Lock" syntax
namespace lockTask.Data;

public class TaxRepository
{
    private const decimal TaxRate = 0.10m;

    private readonly string _connectionString;

    public TaxRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task AddSale(decimal amount)
    {
        decimal taxAmount = amount * TaxRate;

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.ExecuteAsync(
                "INSERT INTO sales (amount, tax_amount, is_tax_paid) " +
                "VALUES (@amount, @taxAmount, FALSE)",
                new { amount, taxAmount }
            );
        }
    }

    public async Task<decimal> GetUnpaidTaxSum()
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            var taxes = await connection.QueryAsync<decimal>(
                "SELECT tax_amount FROM sales WHERE is_tax_paid = FALSE"
            );

            decimal unpaidTax = 0;

            foreach (var tax in taxes)
            {
                unpaidTax += tax;
            }

            return unpaidTax;
        }
    }

    public async Task PayTax()
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var transaction = await connection.BeginTransactionAsync())
            {
                var taxes = await connection.QueryAsync<decimal>(
                    "SELECT tax_amount FROM sales " +
                    "WHERE is_tax_paid = FALSE " +
                    "FOR UPDATE",
                    transaction: transaction
                );

                decimal unpaidTax = 0;

                foreach (var tax in taxes)
                {
                    unpaidTax += tax;
                }

                if (unpaidTax == 0)
                {
                    await transaction.CommitAsync();
                    return;
                }

                await connection.ExecuteAsync(
                    "INSERT INTO tax_payments (amount, paid_at) " +
                    "VALUES (@unpaidTax, @paidAt)",
                    new
                    {
                        unpaidTax,
                        paidAt = DateTime.Now
                    },
                    transaction: transaction
                );

                await connection.ExecuteAsync(
                    "UPDATE sales " +
                    "SET is_tax_paid = TRUE " +
                    "WHERE is_tax_paid = FALSE",
                    transaction: transaction
                );

                await transaction.CommitAsync();
            }
        }
    }

    public async Task DisplayTaxPayments()
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            var payments = await connection.QueryAsync(
                "SELECT * FROM tax_payments"
            );

            foreach (var payment in payments)
            {
                Console.WriteLine(
                    $"Payment ID: {payment.id} | " +
                    $"Amount: {payment.amount} | " +
                    $"Paid At: {payment.paid_at}"
                );
            }
        }
    }
}