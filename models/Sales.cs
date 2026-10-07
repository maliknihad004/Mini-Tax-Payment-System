namespace lockTask.Models;

public class Sales
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsTaxPaid { get; set; }
}