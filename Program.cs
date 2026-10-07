using DotNetEnv;
using lockTask.Data;
//chatgpt wrote these tests.
Env.Load();

string connectionString =
    Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? throw new Exception("Database connection string was not found.");

var repository = new TaxRepository(connectionString);

await repository.AddSale(100);
await repository.AddSale(200);
await repository.AddSale(200);

decimal unpaidTax = await repository.GetUnpaidTaxSum();

Console.WriteLine($"Unpaid Tax before payment: {unpaidTax}");

var task1 = Task.Run(() => repository.PayTax());
var task2 = Task.Run(() => repository.PayTax());

await Task.WhenAll(task1, task2);

await repository.DisplayTaxPayments();
