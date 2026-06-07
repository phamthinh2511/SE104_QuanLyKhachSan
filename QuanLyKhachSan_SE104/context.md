\# Project Context: QuanLyKhachSan\_SE104



\## Technology Stack

\- \*\*Frontend / UI:\*\* WPF (Windows Presentation Foundation), XAML

\- \*\*Architecture Pattern:\*\* MVVM (Model-View-ViewModel)

\- \*\*Database Layer:\*\* Entity Framework Core (EF Core) 

\- \*\*Database Management System:\*\* MySQL

\- \*\*Thread \& Synchronization:\*\* Asynchronous programming with Dispatcher for UI synchronization.



\## System Global Components

\- \*\*Database Context:\*\* `QuanLyKhachSanContext` manages all interactions with MySQL.

\- \*\*Global Event Bus:\*\* `HotelEventBus` provides event aggregation. It contains `PublishRoomStatusChanged()` to notify components when a room's state updates.

\- \*\*UI Thread Dispatcher:\*\* Event handlers updating the UI must be invoked via `Application.Current.Dispatcher.Invoke(LoadData)`.



\## Core Domains Affected

1\. \*\*Booking Operations (`DatPhong`):\*\* Managed by `DatPhongViewModel.cs` and `DatPhongPage.xaml`. Handles normal reservations, walk-ins, extensions, and room transfers.

2\. \*\*Room Management \& Details (`Phong` \& `ChiTietPhong`):\*\* Managed by `PhongViewModel.cs` and `ChiTietPhongViewModel.cs`. Tracks room availability, cleaning statuses, and live details.



\## Implementation Guardrails

\- \*\*DbContext Lifetime:\*\* Transitioning from long-lived, window-scoped DbContexts to short-lived, atomic unit-of-work DbContexts (`using var ctx = new QuanLyKhachSanContext();`).

\- \*\*Separation of Concerns:\*\* ViewModel layers must only hold UI-bindable properties, handle `ICommand` executions, and route user feedback (e.g., `MessageBox.Show`). Business logic, database queries, transactions, and internal status validations must reside in DAL layers.

