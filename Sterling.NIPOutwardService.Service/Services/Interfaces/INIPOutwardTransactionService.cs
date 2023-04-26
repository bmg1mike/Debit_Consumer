namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public partial interface INIPOutwardTransactionService 
{
    Task<FundsTransferResult<NIPOutwardTransaction>> Create(CreateNIPOutwardTransactionDto request);
    FundsTransferResult<NIPOutwardTransaction> ValidateCreateNIPOutwardTransactionDto(CreateNIPOutwardTransactionDto request);
    OutboundLog GetOutboundLog();
    Task<int> Update(NIPOutwardTransaction request);
    Task<Result<TransactionValidationResponseDto>> CheckIfTransactionIsSuccessful(TransactionValidationRequestDto request);
}