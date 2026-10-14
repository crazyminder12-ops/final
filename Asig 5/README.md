# Assignment 5 — Delivery System

## 1. Project topic
**Delivery System** calculates delivery prices for customers.

## 2. Intended users
The program can be used by a small delivery service to estimate the price of a delivery.

## 3. Input fields
Each delivery request contains:
- `CustomerName` — customer's name. It cannot be empty.
- `DistanceKm` — delivery distance in kilometres. It cannot be negative.

Invalid values are rejected by the `DeliveryRequest` constructor with a clear error message. The service accepts deliveries only up to 50 km; requests beyond 50 km are excluded.

## 4. Business rules
Two pricing policies are available:

- **Standard delivery:** 500 KZT base price + 100 KZT for each kilometre.
- **Express delivery:** 800 KZT base price + 150 KZT for each kilometre.

Example for a 2 km delivery:
- Standard: 500 + 100 × 2 = **700 KZT**
- Express: 800 + 150 × 2 = **1100 KZT**

The program calculates prices for the same dataset using both policies and sorts the quotes from cheapest to most expensive.

## 5. Pipeline stages
The workflow is made of three transformation stages:

1. `SelectValidRequests` — selects requests within the delivery area (0–50 km). Requests over 50 km are excluded. Input: a list of `DeliveryRequest`; output: a new list of `DeliveryRequest`.
2. `CalculatePrices` — calculates a price for every request using a policy passed as `Func<DeliveryRequest, decimal>`. Input: requests and a policy; output: a list of `DeliveryQuote`.
3. `SortByPrice` — sorts quotes by price, cheapest first. Input: a list of `DeliveryQuote`; output: a new sorted list.

`RunPipeline` connects the three stages. The pricing policy is selected outside the pipeline and passed into it, so the pipeline code stays the same.

## 6. Automated tests
There are **8 automated xUnit tests** in `DeliverySystem.Tests/DeliveryServiceTests.cs`:

1. `NormalWorkflow_CalculatesAndSortsQuotes` — checks price calculation and sorting.
2. `EmptyDataset_ReturnsNoQuotes` — checks that an empty list returns no quotes without crashing.
3. `BoundaryValue_ZeroDistanceCostsBasePrice` — checks the minimum distance of 0 km.
4. `NegativeDistance_IsRejected` — checks that a negative distance is rejected.
5. `TwoPolicies_ReturnDifferentPrices` — checks Standard and Express results for the same 2 km request.
6. `SelectingRequests_DoesNotChangeOriginalList` — checks that the original list is not changed.
7. `RequestOver50Km_IsNotSelected` — checks that a request over the 50 km limit is excluded.
8. `MissingList_IsRejected` — checks that a missing list is rejected.

## 7. Team members and contributions
**Liman Kenzhebekov**
- Implemented the main delivery workflow and the three pipeline stages.
- Implemented the Standard and Express pricing policies.
- Connected the stages in `RunPipeline` and added console output to demonstrate both policies.
- Contributed to the project documentation in this README.

**Yernar Kunanbaev**
- Prepared automated tests for normal, empty, boundary, invalid-input, two-policy, purity, and missing-input cases.
- Helped check the expected prices and test coverage.
- Contributed to the project documentation in this README.

*

## 8. How to run




```bash
dotnet run --project DeliverySystem/DeliverySystem.csproj
```

Run the automated tests:

```bash
dotnet test DeliverySystem.Tests/DeliverySystem.Tests.csproj
```

## 9. Expected demonstration
The console app prints delivery quotes for the Standard and Express policies, using the same sample requests. It also demonstrates an empty dataset.

## 10. Requirement mapping
- Typed input/output models and validation: `DeliverySystem/Program.cs` — `DeliveryRequest`, `DeliveryQuote`; delivery radius rule: maximum 50 km.
- Three pure stages: `SelectValidRequests`, `CalculatePrices`, `SortByPrice`.
- Composed workflow: `RunPipeline`.
- Higher-order function and configurable rule: `CalculatePrices` accepts and calls `Func<DeliveryRequest, decimal>`; `RunPipeline` receives the selected policy.
- Two policies: `StandardPrice`, `ExpressPrice`.
- Automated tests: `DeliverySystem.Tests/DeliveryServiceTests.cs`.
