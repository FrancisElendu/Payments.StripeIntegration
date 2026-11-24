Payments.StripeIntegration
Stripe Gateway Payment System Integration
[https://img.shields.io/badge/build-[TODO]-lightgrey]([TODO: build status URL]) LICENSE.txt [https://img.shields.io/badge/version-[TODO]-lightgrey]([TODO: release URL])
Overview
Payments.StripeIntegration is an ASP.NET Core API for creating Stripe payment intents and receiving Stripe webhook events. It persists payment and webhook data using Entity Framework Core and SQL Server, and uses an outbox with RabbitMQ for message delivery. This project provides a starting point for integrating Stripe payments into .NET applications.
Features
•	Creates Stripe PaymentIntents through an API endpoint.
•	Returns a payment ID and Stripe client secret.
•	Receives Stripe webhook events and verifies their signatures.
•	Avoids duplicate webhook event processing.
•	Persists payments, webhook logs, and outbox messages.
•	Uses RabbitMQ to publish messages from the outbox.
Tech Stack
•	Language: C#
•	Framework: ASP.NET Core, targeting .NET 9 (net9.0)
•	Payment processing: Stripe.net
•	Data access: Entity Framework Core and SQL Server
•	Messaging: RabbitMQ
•	Application patterns: MediatR and outbox
The project files target .NET 9, although the project context supplied for this README mentioned .NET Core 8.
Prerequisites
•	.NET 9 SDK
•	SQL Server instance
•	RabbitMQ instance
•	Stripe account with API credentials
•	Stripe webhook signing secret for webhook handling
Installation
1.	Clone the repository:
git clone https://github.com/FrancisElendu/Payments.StripeIntegration.git
   cd Payments.StripeIntegration
  	2.	Restore and build the API project:
  	dotnet restore src/Payments.StripeIntegration.Api/Payments.StripeIntegration.Api.csproj
   dotnet build src/Payments.StripeIntegration.Api/Payments.StripeIntegration.Api.csproj --configuration Release
3.	Configure the required settings as described in #configuration.
4.	Run the API:
dotnet run --project src/Payments.StripeIntegration.Api/Payments.StripeIntegration.Api.csproj
Configuration
The application reads configuration through the ASP.NET Core configuration system. Environment variable names below use its standard double-underscore format. The .env example is illustrative; configure your environment or dotenv loader to make these values available to the application.
Variable	Description	Required	Default
Stripe__SecretKey	Stripe secret API key used to create PaymentIntents.	Yes, for payment creation	None
Stripe__WebhookSecret	Signing secret used to validate Stripe webhook events.	Yes, for webhook handling	None
ConnectionStrings__DefaultConnection	SQL Server connection string.	Yes	None
RabbitMQ__Host	RabbitMQ host name.	Yes	None
RabbitMQ__Username	RabbitMQ user name.	Yes	None
RabbitMQ__Password	RabbitMQ password.	Yes	None
RabbitMQ__Port	RabbitMQ port.	No	5672
RabbitMQ__VirtualHost	RabbitMQ virtual host.	No	/
Example .env values (replace placeholders with settings for your environment):
Stripe__SecretKey=sk_test_[TODO]
Stripe__WebhookSecret=whsec_[TODO]
ConnectionStrings__DefaultConnection=[TODO: SQL Server connection string]
RabbitMQ__Host=[TODO: RabbitMQ host]
RabbitMQ__Username=[TODO: RabbitMQ username]
RabbitMQ__Password=[TODO: RabbitMQ password]
RabbitMQ__Port=5672
RabbitMQ__VirtualHost=/
Usage
curl -X POST "https://localhost:5001/api/payments/create-intent" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 10.00,
    "currency": "usd",
    "customerId": "",
    "description": "Example payment"
  }'
Common commands:
dotnet restore src/Payments.StripeIntegration.Api/Payments.StripeIntegration.Api.csproj
dotnet build src/Payments.StripeIntegration.Api/Payments.StripeIntegration.Api.csproj
dotnet run --project src/Payments.StripeIntegration.Api/Payments.StripeIntegration.Api.csproj
dotnet test tests/API.Tests/API.Tests.csproj
Project Structure
Payments.StripeIntegration/
├── src/
│   ├── Payments.StripeIntegration.Api/             # ASP.NET Core API and controllers
│   ├── Payments.StripeIntegration.Application/      # Commands, handlers, and interfaces
│   ├── Payments.StripeIntegration.Domain/           # Domain entities, events, and enums
│   ├── Payments.StripeIntegration.Infrastructure/   # Stripe, persistence, outbox, and RabbitMQ
│   └── Payments.StripeIntegration.Shared/           # Shared project components
├── tests/
│   └── API.Tests/                                   # API tests
└── LICENSE.txt                                      # MIT License
API Reference
Method	Endpoint	Description	Request
POST	/api/payments/create-intent	Creates a Stripe PaymentIntent and returns the payment ID and client secret.	JSON object with amount, currency, customerId, and description.
POST	/api/stripe/webhook	Receives Stripe webhook events and validates the Stripe-Signature header.	Stripe webhook payload and signature header.
Testing
dotnet test tests/API.Tests/API.Tests.csproj
Webhook tests use Stripe webhook test helpers. Configure any required test settings in the test environment; do not use live Stripe credentials for tests.
Roadmap
•	[ ] [TODO: Add planned work]
•	[ ] [TODO: Add planned work]
•	[ ] [TODO: Add planned work]
Contributing
Open an issue to discuss a proposed change, then submit a pull request with a concise description of the change and relevant tests. Run the test suite before submitting.
License
This project is licensed under the MIT License. See LICENSE.txt for details.
