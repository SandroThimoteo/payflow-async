// Validar o status do pagamento, para garantir que ele esteja em um estado válido antes de processar ou atualizar o pagamento.
namespace Payment.Domain.Enums;

public enum PaymentStatus
{   

    
        Pending,
        Processing,
        Approved,
        Rejected,
        Failed
    
}