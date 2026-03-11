# PayFlow Async

**Asynchronous Payment Processing Platform**

A distributed payment platform built with .NET 8, MySQL, and AWS SQS, designed after architectural practices common in modern financial institutions — bounded contexts, event-driven integration, domain protection, and observability by default.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [API Reference](#api-reference)
- [Payment Flow](#payment-flow)
- [Domain Rules](#domain-rules)
- [Running Tests](#running-tests)
- [Environment Variables](#environment-variables)

---

## Overview

PayFlow Async ingests payment requests synchronously via REST API, processes them asynchronously via a background worker, and exposes reliable, auditable outcomes to downstream consumers.

**Key design principles:**
- The API responds in milliseconds — the client never waits for gateway authorization
- The Worker processes payments independently and is fully idempotent
- Failed messages are automatically retried; unrecoverable ones are routed to a Dead Letter Queue
- The domain layer protects all business invariants — invalid state transitions are impossible

---

## Architecture

### System Flow

```
Client
  │
  └─► POST /payments
        │
        ├─ Validate idempotency (ExternalReference must be unique)
        ├─ Create Payment aggregate (status: Pending)
        ├─ Persist to MySQL
        ├─ Publish { paymentId } to SQS
        └─ Return 201 Created ◄── client is free immediately

                          SQS Queue
                               │
                    Worker polls for messages
                               │
                    Fetch Payment from MySQL
                               │
                    MarkAsProcessing → persist
                               │
                    Call external Gateway
                               │
              ┌────────────────┼────────────────┐
           Approved         Rejected          Failed
              │                │                │
        MarkAsApproved   MarkAsRejected   MarkAsFailed
              └────────────────┼────────────────┘
                               │
                    Persist final status
                               │
                    Delete message from SQS
```

### Layer Responsibilities

| Layer | Project | Responsibility |
|-------|---------|----------------|
| Domain | `Payment.Domain` | Business rules, entity invariants, valid status transitions |
| Application | `Payment.Application` | Use case orchestration, interfaces, DTOs |
| Infrastructure | `Payment.Infrastructure` | EF Core, MySQL, SQS publisher |
| API | `Payment.Api` | HTTP endpoints, dependency wiring |
| Worker | `Payment.Processor` | SQS consumer, async payment processing |

---

## Tech Stack

- **.NET 8** — API and Worker
- **ASP.NET Core Minimal APIs** — lightweight HTTP layer
- **Entity Framework Core 8** + **Pomelo MySQL** — data persistence
- **AWS SQS** — async message queue (LocalStack for local development)
- **Docker** — LocalStack container
- **xUnit** — unit testing

---

## Project Structure

```
payflow-async/
├── src/
│   ├── Payment.Api/                  # REST API (POST /payments, GET /payments/{id})
│   │   ├── Program.cs                # App bootstrap, DI registration, endpoints
│   │   └── appsettings.json
│   │
│   ├── Payment.Processor/            # Background Worker (SQS consumer)
│   │   ├── Worker.cs                 # Long polling, message processing, retry logic
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── Payment.Application/          # Use cases and abstractions
│   │   ├── Abstractions/
│   │   │   ├── IPaymentRepository.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── IMessagePublisher.cs
│   │   └── Payments/
│   │       ├── Commands/             # CreatePaymentCommand + Handler
│   │       └── DTOs/                 # Request and Response records
│   │
│   ├── Payment.Domain/               # Core business logic (no external dependencies)
│   │   ├── Entities/Payment.cs       # Payment aggregate with status transitions
│   │   ├── Enums/PaymentStatus.cs    # Pending → Processing → Approved/Rejected/Failed
│   │   └── Exceptions/DomainException.cs
│   │
│   └── Payment.Infrastructure/       # External concerns
│       ├── Persistence/PaymentDbContext.cs
│       ├── Repositories/PaymentRepository.cs
│       └── Messaging/SqsMessagePublisher.cs
│
└── tests/
    └── Payment.Tests/
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)
- [MySQL 8](https://dev.mysql.com/downloads/)
- [AWS CLI](https://aws.amazon.com/cli/)

### 1. Clone and configure

```bash
git clone https://github.com/your-username/payflow-async.git
cd payflow-async
```

Edit the connection string in both `appsettings.json` files:

```
src/Payment.Api/appsettings.json
src/Payment.Processor/appsettings.json
```

```json
"DefaultConnection": "Server=localhost;Database=payflow;User=root;Password=YOUR_PASSWORD;"
```

### 2. Create the database

```sql
CREATE DATABASE payflow;
```

### 3. Run migrations

```bash
cd src/Payment.Api
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project ../Payment.Infrastructure --startup-project .
dotnet ef database update --project ../Payment.Infrastructure --startup-project .
```

### 4. Start LocalStack (SQS)

```bash
docker run -d -p 4566:4566 --name localstack localstack/localstack
```

Configure fake credentials for LocalStack:

```bash
aws configure
# Access Key: test
# Secret Key: test
# Region: us-east-1
# Output: json
```

Create the queue:

```bash
aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name payflow-payments --region us-east-1
```

### 5. Run the API

```bash
cd src/Payment.Api
dotnet run
# Swagger: http://localhost:5062/swagger
```

### 6. Run the Worker

Open a second terminal:

```bash
cd src/Payment.Processor
dotnet run
```

---

## API Reference

### Create Payment

```
POST /payments
```

**Request body:**

```json
{
  "externalReference": "ORDER-2026-0001",
  "customerId": "CLIENT-123",
  "amount": 150.00,
  "currency": "BRL",
  "method": "CREDIT_CARD"
}
```

**Response `201 Created`:**

```json
{
  "id": "8f2a1d1c-32a1-4c10-9d9e-123456789abc",
  "externalReference": "ORDER-2026-0001",
  "customerId": "CLIENT-123",
  "amount": 150.00,
  "currency": "BRL",
  "method": "CREDIT_CARD",
  "status": "Pending",
  "createdAtUtc": "2026-03-09T14:00:00Z",
  "updatedAtUtc": null
}
```

---

### Get Payment

```
GET /payments/{id}
```

**Response `200 OK`:**

```json
{
  "id": "8f2a1d1c-32a1-4c10-9d9e-123456789abc",
  "externalReference": "ORDER-2026-0001",
  "customerId": "CLIENT-123",
  "amount": 150.00,
  "currency": "BRL",
  "method": "CREDIT_CARD",
  "status": "Approved",
  "createdAtUtc": "2026-03-09T14:00:00Z",
  "updatedAtUtc": "2026-03-09T14:00:03Z"
}
```

**Supported payment methods:** `CREDIT_CARD`, `DEBIT_CARD`, `PIX`, `BANK_TRANSFER`

---

## Payment Flow

### Status transitions

```
Pending ──► Processing ──► Approved
                      └──► Rejected
                      └──► Failed
```

Invalid transitions throw a `DomainException`. For example, a payment cannot go from `Pending` directly to `Approved` — it must pass through `Processing` first.

### Retry and Dead Letter Queue

If the Worker fails to process a message, it does **not** delete it from the queue. SQS will automatically re-deliver it after the `VisibilityTimeout` (30s) expires. After a configurable number of failed attempts, the message is automatically routed to the Dead Letter Queue for investigation.

---

## Domain Rules

- `ExternalReference` must be unique — duplicate requests return an error
- `Amount` must be greater than zero
- `CustomerId`, `Currency`, and `Method` cannot be null or empty
- Status transitions are strictly enforced by the domain entity
- The Worker is fully idempotent — reprocessing a non-`Pending` payment is a no-op

---

## Running Tests

```bash
cd tests/Payment.Tests
dotnet test
```

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | MySQL connection string | — |
| `Aws__PaymentQueueName` | SQS queue name | `payflow-payments` |
| `Aws__ServiceUrl` | SQS endpoint override (LocalStack) | — (uses real AWS) |

For local development, `appsettings.Development.json` overrides `ServiceUrl` to `http://localhost:4566` automatically.

---

## License

MIT

---

## Roadmap

The codebase is intentionally structured to make the AWS production migration straightforward. The local environment already mirrors production — the only difference is the SQS endpoint.

### ✅ Done (local environment)
- REST API with idempotency and domain validation
- Async processing via SQS (LocalStack)
- MySQL persistence with EF Core migrations
- Worker with long polling, retry, and DLQ routing
- Full Clean Architecture separation — domain has zero external dependencies

### 🔜 Next step — AWS integration

The project is ready for AWS. No code changes are required — only configuration:

**1. Create the real SQS queue on AWS:**
```bash
aws sqs create-queue --queue-name payflow-payments --region us-east-1
```

**2. Create the Dead Letter Queue and attach it:**
```bash
aws sqs create-queue --queue-name payflow-payments-dlq --region us-east-1

# Attach DLQ with maxReceiveCount=3 (after 3 failures, route to DLQ)
aws sqs set-queue-attributes \
  --queue-url <PAYMENTS_QUEUE_URL> \
  --attributes '{"RedrivePolicy":"{\"deadLetterTargetArn\":\"<DLQ_ARN>\",\"maxReceiveCount\":\"3\"}"}'
```

**3. Remove the `ServiceUrl` override** from `appsettings.json` — the SDK will automatically use real AWS:
```json
"Aws": {
  "PaymentQueueName": "payflow-payments",
  "ServiceUrl": ""
}
```

**4. Set real AWS credentials** via environment variables or IAM Role (recommended for ECS/EC2):
```bash
export AWS_ACCESS_KEY_ID=your_key
export AWS_SECRET_ACCESS_KEY=your_secret
export AWS_REGION=us-east-1
```

### 🔜 Planned improvements
- [ ] Deploy to AWS ECS (API + Worker as separate services)
- [ ] RDS MySQL (replace local MySQL)
- [ ] SNS fan-out for `PaymentApproved` / `PaymentFailed` events
- [ ] Outbox Pattern — guarantee atomic DB write + SQS publish
- [ ] OpenTelemetry tracing across API and Worker
- [ ] CloudWatch dashboards and alerting