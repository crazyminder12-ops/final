# Delivery System — Assignments 5–8

**Group:** IT-2505
**Team:** Liman Kenzhebekov, Yernar Kunanbaev
**Stack:** C# / .NET 8 console application, xUnit tests (Assignment 8 also uses C and P/Invoke)

One project built up over four assignments. Each assignment continues the previous one. The domain stays the same throughout: a small delivery service that validates delivery requests and calculates prices in KZT.

---

## Contents

1. [Project overview](#project-overview)
2. [Repository layout](#repository-layout)
3. [Business rules](#business-rules-shared-by-all-assignments)
4. [Assignment 5 — Pure pipeline and higher-order functions](#assignment-5--pure-pipeline-and-higher-order-functions)
5. [Assignment 6 — Validated types and safe state transitions](#assignment-6--validated-types-and-safe-state-transitions)
6. [Assignment 7 — Exception handling in FP (Result / Option)](#assignment-7--exception-handling-in-fp-result--option)
7. [Assignment 8 — LINQ, currying, deferred execution, resources, native interop](#assignment-8--linq-currying-deferred-execution-resources-native-interop)
8. [Build, run and test](#build-run-and-test)
9. [Test summary](#test-summary)
10. [Team contributions](#team-contributions)

---

## Project overview

The program takes delivery requests (customer name and distance), checks that they are valid, and calculates a quote using a pricing policy. Each assignment adds a new functional-programming concept on top of the same domain:

| Assignment | Main topic | Key additions |
|---|---|---|
| **5** | Pure functions, pipelines, higher-order functions | 3-stage pipeline, pricing policy passed as `Func<DeliveryRequest, decimal>` |
| **6** | Making invalid states unrepresentable | Private constructor + factory, `Valid/InvalidDeliveryRequest`, `Pending → Quoted → Failed` states, pattern matching |
| **7** | Functional error handling | `Result<T,E>`, `Option<T>`, `Map` / `Bind`, JSON loading with boundary exception handling |
| **8** | Advanced composition and interop | Extension methods, LINQ reports, currying, `yield return`, `FileStream` ownership, C library through P/Invoke |

---

## Repository layout

```
.
├── README.md                     ← this file
├── Assignment5/                  ← archive "asik 5"
│   ├── DeliverySystem/           Program.cs, DeliverySystem.csproj
│   └── DeliverySystem.Tests/     DeliveryServiceTests.cs (8 tests)
├── Assignment6/                  ← archive "asig 6"
│   ├── DeliverySystem/           Program.cs
│   └── DeliverySystem.Tests/     DeliveryServiceTests.cs (14 tests)
├── Assignment7/                  ← archive "Assignment7_DeliverySystem" (inner folder is also named "asig 6")
│   ├── DeliverySystem/           Program.cs, deliveries.json
│   └── DeliverySystem.Tests/     DeliveryServiceTests.cs (19 tests)
└── Assignment8/                  ← archive "Assignment8_DeliverySystem_Full"
    ├── DeliverySystem/
    │   ├── Domain/               delivery records and domain errors
    │   ├── Functional/           Result, Option, Scoring (currying)
    │   ├── Services/             validation, lookup, pricing, state transition
    │   ├── Extensions/           custom extension methods
    │   ├── Reports/              LINQ reports
    │   ├── Processing/           deferred (yield return) processing
    │   ├── IO/                   JSON load / report export with FileStream
    │   ├── Native/               deliverycalc.c, P/Invoke binding, build scripts
    │   ├── Program.cs
    │   └── deliveries.json
    └── DeliverySystem.Tests/     DeliveryFeatureTests.cs, NativeAndFileTests.cs (16 tests)
```

Each assignment folder is a separate, self-contained solution (an app project and a test project). Generated `bin/` and `obj/` folders are not part of the source and can be deleted.

---

## Business rules (shared by all assignments)

- The customer name must not be empty.
- The delivery distance must be from **0 to 50 km**, inclusive. A distance of `0` is valid.
- **Standard delivery:** 500 KZT + 100 KZT per km.
- **Express delivery:** 800 KZT + 150 KZT per km.
- Quotes are sorted from cheapest to most expensive.

Example for a 2 km delivery:

| Policy | Calculation | Price |
|---|---|---|
| Standard | 500 + 100 × 2 | **700 KZT** |
| Express | 800 + 150 × 2 | **1100 KZT** |

---

## Assignment 5 — Pure pipeline and higher-order functions

**Goal:** build the delivery workflow from small pure functions and make the pricing rule configurable.

**Models:** `DeliveryRequest` (`CustomerName`, `DistanceKm`) and `DeliveryQuote`. The constructor rejects an empty name or a negative distance with a clear error message.

**Pipeline stages:**

1. `SelectValidRequests` keeps requests within the 0–50 km delivery area and returns a **new** list.
2. `CalculatePrices` takes the pricing policy as `Func<DeliveryRequest, decimal>` and returns a list of `DeliveryQuote`.
3. `SortByPrice` returns a new list sorted from cheapest to most expensive.

`RunPipeline` connects the three stages. The policy (`StandardPrice` or `ExpressPrice`) is chosen outside the pipeline and passed in, so the pipeline code does not change.

| Requirement | Implementation |
|---|---|
| Typed input/output models and validation | `DeliveryRequest`, `DeliveryQuote` |
| Three pure stages | `SelectValidRequests`, `CalculatePrices`, `SortByPrice` |
| Composed workflow | `RunPipeline` |
| Higher-order function | `CalculatePrices` accepts and calls a `Func<DeliveryRequest, decimal>` |
| Two policies | `StandardPrice`, `ExpressPrice` |

**Demonstration:** the console prints Standard and Express quotes for the same sample data, then shows an empty dataset.

---

## Assignment 6 — Validated types and safe state transitions

**Goal:** make the workflow safe so that invalid data cannot enter it and methods cannot be called in the wrong order.

1. **Validated factory.** The `DeliveryRequest` constructor is private. Requests are created only through `DeliveryRequest.Create(...)`, which returns:
   - `ValidDeliveryRequest` when the data is correct;
   - `InvalidDeliveryRequest` with a list of validation errors otherwise.
2. **Explicit delivery states** instead of flags or mutable fields:
   - `PendingDelivery`: waiting for a quote;
   - `QuotedDelivery`: a quote has already been created;
   - `FailedDelivery`: the operation failed.
3. **Pattern matching.** `CalculateQuote` uses a `switch` on the current state.
4. **Safe workflow:**

   ```
   PendingDelivery ──► QuotedDelivery
   QuotedDelivery  ──► FailedDelivery("This delivery has already been quoted.")
   ```

   A repeated quote request returns `FailedDelivery`, and the original state is not changed.

The Assignment 5 pipeline (`SelectValidRequests → CalculatePrices → SortByPrice → RunPipeline`) and both pricing policies are kept.

**Demonstration:** Standard and Express prices, rejection of an invalid request, successful creation of a quote, and rejection of a repeated quote.

---

## Assignment 7 — Exception handling in FP (Result / Option)

**Goal:** represent expected failures explicitly instead of throwing exceptions.

| Requirement | Implementation |
|---|---|
| Explicit success/failure | `Result<T,E>` with `Success` and `Failure` cases |
| Domain failures | `INVALID_CUSTOMER`, `INVALID_DISTANCE`, `DELIVERY_NOT_FOUND` |
| Missing value model | `Option<T>` with `Some` / `None`. `FindRequest` looks up a customer by name, case-insensitively |
| Required lookup | `RequireRequest` converts `None` into `DELIVERY_NOT_FOUND` |
| `Map` | Transforms a `DeliveryQuote` into a `QuoteSummary` |
| `Bind` | Continues from a lookup into `CreateQuote`, which can also fail |
| Failure propagation | `Map` and `Bind` return the existing failure without calling the function |
| Operational exceptions | `LoadFromJson` catches file-not-found, directory, I/O and malformed-JSON errors **only at the boundary** and returns `FILE_NOT_FOUND` / `MALFORMED_JSON` |
| No partial update | JSON is fully read and deserialized before the in-memory list is built |

**Workflow:**

```
LoadFromJson → CreateRequest (validate) → FindRequest (Option)
            → RequireRequest → Bind(CreateQuote) → Map(to summary) → print once
```

**Map vs Bind:** `Map` takes a normal function `T → U`. `Bind` takes a function that already returns a `Result` (`T → Result<U,E>`), so you don't end up with a nested `Result<Result<…>>`.

**Expected console output:**

```text
Aruzhan | 2 km | 700 KZT
DELIVERY_NOT_FOUND        ← lookup for customer "Unknown"
FILE_NOT_FOUND            ← loading "missing-deliveries.json"
```

---

## Assignment 8 — LINQ, currying, deferred execution, resources, native interop

**Goal:** split the project into modules and add advanced composition techniques plus a native C function.

| # | Topic | Implementation |
|---|---|---|
| 1 | Extension methods | `WhereDistanceAtLeast` filters an `IEnumerable<T>` using a distance selector. `MapFailureAware` builds on the existing `Result<T,E>` |
| 2 | LINQ reports | `LongDistance`: deliveries above a threshold, ordered by distance (descending), then by customer name. `TotalsByCategory`: groups by category and sums prices, ordered by total (descending), then by category |
| 3 | Currying / partial application | `Scoring.CreateScore(config)` returns a function. `Economy` (100 + 10/km) and `Priority` (250 + 25/km) give different scores for the same request |
| 4 | Deferred execution | `DeferredDeliveryProcessor.Stream` uses `yield return`, so nothing runs until enumeration. `Snapshot` calls `ToArray()` once to get a stable result that can be reused |
| 5 | Resource ownership | `DeliveryFileStore.Load` / `Export` open and dispose their `FileStream` with `using`, including when an exception is thrown |
| 6 | Memory / lifetime | Short-lived enumerables. `ToArray()` is the explicit snapshot point. No caches or event subscriptions |
| 7–9 | Native interop | `deliverycalc.c` exports `int delivery_surcharge_cents(int distance_meters, int rate_cents_per_km)`. C# imports it with `[DllImport("deliverycalc", CallingConvention = Cdecl)]`. The `NativeFee` adapter checks inputs (0–50,000 m, non-negative rate) **before** the call and turns `DllNotFoundException` / `EntryPointNotFoundException` into `Result` failures |

Native example: 2,000 m at 100 cents/km returns **200 cents**.

Native error codes: `NATIVE_INVALID_INPUT`, `NATIVE_LIBRARY_NOT_FOUND`, `NATIVE_ENTRY_NOT_FOUND`.

**Sample data (`deliveries.json`):** Aruzhan 2 km, Ali 12 km, Dana 18 km, Ernar 12 km, Miras 4 km, in the categories Standard, Express and Economy.

**Demonstration:** a long-distance report, totals by category, the snapshot size, and the native surcharge (or a message that the native library hasn't been built).

---

## Build, run and test

**Prerequisites:** .NET 8 SDK. For Assignment 8's native part, you also need GCC (MinGW-w64 on Windows).

### Assignments 5, 6, 7

Run from the assignment's folder:

```bash
dotnet build DeliverySystem/DeliverySystem.csproj
dotnet run   --project DeliverySystem/DeliverySystem.csproj
dotnet test  DeliverySystem.Tests/DeliverySystem.Tests.csproj
```

### Assignment 8

First build the native library.

**Windows (PowerShell):**

```powershell
cd DeliverySystem/Native
.\build-windows.bat          # produces deliverycalc.dll
cd ../..
```

**Linux:**

```bash
cd DeliverySystem/Native
sh build-linux.sh            # produces libdeliverycalc.so
cd ../..
```

The library has to be on the application's load path (for example, copied next to the built `DeliverySystem` executable). Then run:

```bash
dotnet build DeliverySystem/DeliverySystem.csproj
dotnet test  DeliverySystem.Tests/DeliverySystem.Tests.csproj
dotnet run   --project DeliverySystem/DeliverySystem.csproj
```

If the native library is missing, the application still runs and prints that the native call is unavailable.

---

## Test summary

All tests use xUnit. File tests create their own temporary files and delete them in `finally`, so they don't depend on any particular machine.

| Assignment | Tests | What is covered |
|---|---|---|
| 5 | 8 | Normal workflow and sorting, empty dataset, 0 km boundary, negative distance, two policies, original list unchanged, > 50 km excluded, missing list |
| 6 | 14 | Valid/invalid factory results, empty name, invalid distance, 0 and 50 km boundaries, Standard/Express pricing, sorting, empty input, safe transitions, repeated quote rejected, original state preserved, null input |
| 7 | 19 | Validation, `Some`/`None` lookup, 0 km is valid, `None` → domain failure, `Map`/`Bind` success and failure propagation, integrated workflow, missing/malformed/valid JSON, Assignment 6 pipeline and state-transition regression |
| 8 | 16 | Extension methods, reports (empty input, no matches, tie ordering, grouping), curried configurations, deferred execution, snapshot reuse, validator, lookup, native input validation, missing file, malformed JSON, valid JSON |
| **Total** | **57** | |

---

## Team contributions

### Liman Kenzhebekov
- **A5:** main workflow, the three pipeline stages, Standard/Express policies, `RunPipeline`, console demo.
- **A6:** domain model, validated factory, delivery state transitions, pattern matching, console workflow.
- **A7:** `Result<T,E>`, `Map`/`Bind` with failure propagation, validation and quote workflow, integration into the console app, tests for domain outcomes and Map/Bind.
- **A8:** extension methods, LINQ reports, curried scoring, deferred processing and the related tests.

### Yernar Kunanbaev
- **A5:** automated tests (normal, empty, boundary, invalid input, two policies, purity, missing input) and checking expected prices.
- **A6:** automated tests for valid/invalid input, boundaries, normal workflow, repeated quote and state preservation; README review.
- **A7:** `Option<T>` lookup flow, JSON input and boundary exception handling, tests for lookups and file errors; README and demo review.
- **A8:** C native function, P/Invoke binding and adapter, native build scripts, tests for the native and file boundaries.

**Both members:** design discussion, integration review, end-to-end runs and defense preparation. Each member can explain and modify their own part during the individual defense.
