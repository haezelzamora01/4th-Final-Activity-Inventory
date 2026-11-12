# Inventory Management System

A C# Windows Forms inventory application targeting .NET 10. It provides account registration and sign-in with PBKDF2 password hashing, an Access-backed dashboard, and inventory CRUD operations.

## Requirements

- Visual Studio Community with the .NET desktop development workload
- .NET 10 SDK/runtime
- Microsoft Access Database Engine matching the application's platform
- The included `Database/InventoryDatabase.accdb` file

The project currently uses `Microsoft.ACE.OLEDB.12.0`. If Access reports that the provider is not registered, install the Access Database Engine with the same 32-bit or 64-bit bitness as the application. In Visual Studio, check the project's platform target under **Project > Properties > Build**. Office and the Access Database Engine should use compatible bitness. Database operations now report this provider/bitness guidance when the connection cannot be opened.

## Run the application

1. Open `InventoryManagementSystem.slnx` in Visual Studio.
2. Build the solution.
3. Confirm that `Database/InventoryDatabase.accdb` is present.
4. Press **F5**.
5. Use **Create account** to register a test account, then sign in.

The database is copied to the build output by the project file. Do not store real passwords or personal information in a repository or screenshot.

## Access tables expected by the application

The dashboard keeps the summary, add/edit form, search, grid, and CRUD actions on one screen. Its modern layout contains a responsive header, five status cards, a single product editor, and a searchable DataGridView product list. The summary cards show Total Products, Total Value, In Stock, Low Stock, and Out of Stock. The product list also shows each product's Date Added and refreshes the totals after changes. The account features require a `Users` table with:

- `UserID` - AutoNumber primary key
- `FullName` - Short Text
- `Username` - Short Text with a unique index
- `PasswordHash` - Short Text
- `UserRole` - Short Text
- `IsActive` - Yes/No

Inventory CRUD requires a `Products` table with:

- `ProductID` - AutoNumber primary key
- `ProductName` - Short Text
- `Description` - Short Text (added automatically on first run if missing)
- `Category` - Short Text
- `Supplier` - Short Text
- `Quantity` - Number (Integer)
- `Price` - Currency (shown as Unit Price)
- `CreatedAt` - Date/Time (added automatically on first run if missing)

The optional Stock In and Stock Out actions also require `StockTransactions` with `ProductID`, `TransactionType`, `Quantity`, `TransactionDate`, and `UserID`. These actions are not complete unless that table exists and is related to the other tables.

Do not create extra tables unless they are required by the teacher's instructions. Verify the table names and field names in Access before testing.

## Functional test checklist

- Register with empty fields, mismatched passwords, and a password shorter than eight characters.
- Register a valid test account and verify that the password is not stored as plain text.
- Attempt to register the same username twice.
- Sign in with a correct and incorrect password.
- Verify that an inactive account is rejected.
- Add, display, search, edit, and delete an inventory item.
- Confirm the dashboard cards update after adding, editing, deleting, and refreshing records.
- The product list shows a continuous display number. The hidden Access `ProductID` remains the permanent database key, so deleting a record does not incorrectly renumber or reuse old database IDs.
- Confirm deletion and verify that the record changes are saved in Access.
- Test logout and confirm that the sign-in form returns.
- If using Stock In or Stock Out, confirm that both the quantity and transaction record are saved.

## GitHub and screenshots

Before publishing:

1. Use a safe sample database without personal accounts.
2. Never commit passwords, exported CSV files, credentials, or personal information.
3. Capture screenshots of sign-in, registration validation, dashboard, inventory CRUD, the Access `Users` table, and the Access inventory tables.
4. Blur or replace usernames and personal names where necessary.
5. Upload the project and copy the repository URL into the teacher's submission form.

The included `.gitignore` excludes Visual Studio output and local export files, but review `git status` before committing.
