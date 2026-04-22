# GLMS Architecture Patterns (Part 2)

This prototype is built as a **modular monolith** (single deployable app) with clear internal layers.

## Video

https://youtu.be/lzY3MJwZ-Yw

## Patterns

### 1) State Pattern
Used to control valid contract status transitions and business rules, as described in Part 1

- `IContractState` defines transition behavior.
- Concrete states: `DraftState`, `ActiveState`, `OnHoldState`, `ExpiredState`.
- `ContractStateFactory` resolves the current behavior from `ContractStatus`.
- `Contract` delegates transitions (`Approve`, `Suspend`, `Resume`, `Expire`) to the current state

## 2) Observer Pattern

Used in `Contract` to publish status change events to subscribers

- `IContractObserver` defines the observer contract
- `ContractStatusChangedEvent` carries event data.
- `Contract` supports `Subscribe`, `Unsubscribe`, and `Notify` for status changes

## 3) Proxy Pattern

Used for currency rate retrieval with caching

- `CurrencyServiceProxy` wraps `ExchangeRateApiService`
- Adds memory caching (`IMemoryCache`) and TTL configuration
- Reduces repeated external API calls

## 4) Service Layer Pattern

Business logic is separated from UI components into dedicated services

- `ClientService`, `ContractService`, `ServiceRequestService`, `DatabaseInitializationService`
- UI pages call service interfaces (`IClientService`, `IContractService`, `IServiceRequestService`, `ICurrencyService`).

## 5) Dependency Injection (IoC)

services are registered and composed in `Program.cs`

- Interface-to-implementation bindings (e.g `IContractService -> ContractService`)
- `IDbContextFactory<AppDbContext>` for data access
- `HttpClient` registration for external currency API

## 6) DTO Pattern

DTOs are used to shape data passed between service layer and UI

examples: `ContractListItemDto`, `ContractTransitionResultDto`, `ContractAgreementUploadResultDto`, `CurrencyRateDto`

## Quick Structure

- `GLMS.Web/Patterns/State` -> State pattern
- `GLMS.Web/Patterns/Observer` -> Observer pattern
- `GLMS.Web/Services` -> Service layer + proxy usage
- `GLMS.Web/DTOs` -> DTO contracts
- `GLMS.Web/Program.cs` -> DI configuration
