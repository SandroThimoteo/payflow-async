#!/bin/bash

echo "Creating SQS queues..."

# Dead Letter Queue
awslocal sqs create-queue \
  --queue-name payflow-payments-dlq \
  --region us-east-1

# Get DLQ ARN
DLQ_ARN=$(awslocal sqs get-queue-attributes \
  --queue-url http://localhost:4566/000000000000/payflow-payments-dlq \
  --attribute-names QueueArn \
  --query 'Attributes.QueueArn' \
  --output text \
  --region us-east-1)

# Main queue with DLQ attached (routes after 3 failed attempts)
awslocal sqs create-queue \
  --queue-name payflow-payments \
  --attributes "{\"RedrivePolicy\":\"{\\\"deadLetterTargetArn\\\":\\\"$DLQ_ARN\\\",\\\"maxReceiveCount\\\":\\\"3\\\"}\"}" \
  --region us-east-1

echo "Queues created successfully:"
awslocal sqs list-queues --region us-east-1
