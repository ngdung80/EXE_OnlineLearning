namespace POT_System_ASPNET.Models;

public class VNPayRequestModel
{
    public int OrderId { get; set; }
    public string FullName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public double Amount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class VNPayResponseModel
{
    public bool Success { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string OrderDescription { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string PaymentId { get; set; } = null!;
    public string TransactionId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string VnPayResponseCode { get; set; } = null!;
}
