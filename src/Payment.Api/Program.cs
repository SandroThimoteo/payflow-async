using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions;
using Payment.Application.Payments.Commands;
using Payment.Application.Payments.DTOs;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Banco de dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Repositório
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => (IUnitOfWork)sp.GetRequiredService<IPaymentRepository>());

// AWS SQS
builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var awsOptions = builder.Configuration.GetSection("Aws");
    var config = new AmazonSQSConfig();

    var serviceUrl = awsOptions["ServiceUrl"];
    if (!string.IsNullOrEmpty(serviceUrl))
        config.ServiceURL = serviceUrl; // LocalStack: http://localhost:4566

    return new AmazonSQSClient(config);
});

builder.Services.AddScoped<IMessagePublisher, SqsMessagePublisher>();
builder.Services.AddScoped<CreatePaymentHandler>(sp => new CreatePaymentHandler(
    sp.GetRequiredService<IPaymentRepository>(),
    sp.GetRequiredService<IUnitOfWork>(),
    sp.GetRequiredService<IMessagePublisher>(),
    builder.Configuration["Aws:PaymentQueueName"] ?? "payflow-payments"
));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// POST /payments — cria pagamento e enfileira no SQS
app.MapPost("/payments", async (
    CreatePaymentRequest request,
    CreatePaymentHandler handler,
    CancellationToken ct) =>
{
    var command = new CreatePaymentCommand(
        request.ExternalReference,
        request.CustomerId,
        request.Amount,
        request.Currency,
        request.Method
    );

    var result = await handler.HandleAsync(command, ct);
    return Results.Created($"/payments/{result.Id}", result);
})
.WithName("CreatePayment")
.WithOpenApi();

// GET /payments/{id} — consulta status do pagamento
app.MapGet("/payments/{id:guid}", async (Guid id, IPaymentRepository repository) =>
{
    var payment = await repository.GetByIdAsync(id);
    if (payment is null)
        return Results.NotFound();

    var response = new PaymentResponse(
        payment.Id,
        payment.ExternalReference,
        payment.CustomerId,
        payment.Amount,
        payment.Currency,
        payment.Method,
        payment.Status.ToString(),
        payment.CreatedAtUtc,
        payment.UpdatedAtUtc
    );

    return Results.Ok(response);
})
.WithName("GetPaymentById")
.WithOpenApi();

app.Run();
