using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IRazorpayCheckoutService
{
    Task<RazorpayOrderResponseDto> CreateOrderAsync(
        Guid userId,
        string userName,
        string userEmail,
        RazorpayOrderRequestDto request,
        CancellationToken cancellationToken);

    Task<RazorpayPaymentVerificationResponseDto> VerifyPaymentAsync(
        Guid userId,
        RazorpayPaymentVerificationRequestDto request,
        CancellationToken cancellationToken);
}
