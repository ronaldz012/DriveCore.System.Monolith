# DriveCore.System.Monolith 🚀

A high-performance **Modular Monolith** built with **.NET 9**, following a custom **Driven Core System** architecture. This project integrates Authentication, Inventory Management, and Branch control using a strictly decoupled approach where each module owns its data.

## 🏗️ Architecture Overview

The project uses a specialized Clean Architecture pattern (Driven Core) applied to modular monoliths:

* **Modular Monolith:** Three distinct domains (**Auth**, **Inventory**, **Branch**) residing in the same codebase but logically separated.
* **Driven Core System:** Each major module is split into:
    * `Data`: Persistence and Database configurations.
    * `UseCases`: Application logic and Business rules.
    * `Infrastructure`: External implementations (Email, Storage, etc).
    * `Contracts`: Public interfaces and DTOs for inter-module communication.
* **Shared Project:** Transversal utilities used across the entire solution.
* **Result Pattern:** Uses a custom `Result<T>` pattern instead of exceptions for flow control, handled by `ToValueOrProblemDetails()` to map results to HTTP 200/400/404/500 responses.

## 📊 System Diagram

```mermaid
graph TD
    API[Web API Project] --> Auth[Auth Module]
    API --> Inv[Inventory Module]
    API --> Br[Branch Module]
    
    subgraph Modules
        Auth --> AuthDB[(PostgreSQL: AuthService)]
        Inv --> InvDB[(PostgreSQL: InventoryService)]
        Br --> BrDB[(PostgreSQL: BranchService)]
    end

    subgraph Core
        Shared[Shared Project] -.-> Auth
        Shared -.-> Inv
        Shared -.-> Br
    end

    style API fill:#f9f,stroke:#333,stroke-width:2px
