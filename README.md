# DriveCore.System 🚀

A robust **Modular Monolith** built with **.NET 8** and **PostgreSQL**, following the **Driven Core System** architecture. This project manages Authentication, Inventory, and Branch infrastructure with a strictly decoupled data approach.

## 🏗️ Architecture: Driven Core
Each module is structured to ensure high maintainability and testability:
* **Data:** Entity Framework Core persistence and PostgreSQL configurations.
* **UseCases:** Pure business logic and application flows.
* **Infrastructure:** Implementations for SMTP, Google OAuth, and external services.
* **Contracts:** DTOs and interfaces for cross-module communication.
* **Shared:** A single transversal project containing the `Result<T>` pattern and global utilities.

## 🔐 Authentication & Verification
The system supports:
* **Google OAuth:** Native integration for seamless sign-in.
* **Email Verification:** Controlled via `PublicAuthentication:Enable`. 
    * If `false`: Users can access the system immediately after signup.
    * If `true`: Users must verify their account via an OTP/Link sent via SMTP before gaining access.

## 📊 Database Architecture
Each module manages its own independent schema within PostgreSQL to prevent tight coupling.

```mermaid
graph LR
    API[System.Api] --> Auth[Auth Module]
    API --> Inv[Inventory Module]
    API --> Br[Branches Module]
    
    subgraph Databases
        Auth --- DB1[(AuthService DB)]
        Inv --- DB2[(InventoryService DB)]
        Br --- DB3[(BranchService DB)]
    end
