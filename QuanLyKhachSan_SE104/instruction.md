Title: REVISED INSTRUCTION FOR BILLING SURCHARGE AND TIMELINE BUGS

CONTEXT AND ROLES
We are maintaining a hotel management system developed in WPF, EF Core, and C#. The specific component requires code refactoring inside the HoaDonService, DashboardViewModel, and any reservation creation components handling walk-ins.

PHASE 1: EARLY CHECK-IN AND WALK-IN TIMELINE ADJUSTMENTS

1. Early Check-In Surcharge Feature

* Problem Statement: A new policy mandates that the standard check-in time is exactly 14:00. If a guest checks in prior to 14:00 on their arrival date, the system must calculate an early check-in surcharge based on the chronological difference between their actual real-time check-in and the 14:00 threshold. Currently, the system fails to capture or bill this duration.
* Resolution Plan: Implement an automated early check-in calculation within HoaDonService. If NgayCheckIn.TimeOfDay is earlier than 14:00 on the day of arrival, compute the early hours. Dynamically register a new service entry in ChiTietDichVu named "Phụ phí check-in sớm" (similar to the existing room extension service structure). The rate per early hour must be driven by the room type's hourly surcharge rate (PhuPhiThemGio).
* Output Requirements: The final bill breakdown must clearly list "Phụ phí check-in sớm" under the service list (DanhSachDichVu) with accurate hours, unit rates, and totals when a guest arrives before 14:00.

2. Walk-In Booking Real-Time Correction

* Problem Statement: When a front-desk agent creates a "Walk-In" booking (immediate check-in), the system erroneously hardcodes or forces the NgayCheckIn property to exactly 14:00 of that day, instead of using the actual real-time timestamp (DateTime.Now). This completely masks early arrivals and breaks the early check-in surcharge mechanism described above.
* Resolution Plan: Locate the reservation or check-in creation logic where Walk-In mode is processed. Remove any forced assignment that overwrites the check-in time to 14:00. Ensure that NgayCheckIn captures exact historical real-world execution time via DateTime.Now.
* Output Requirements: Creating a walk-in booking at 10:00 AM must save the booking check-in timestamp as 10:00 AM in the database, not 14:00.

PHASE 2: DEEP INVESTIGATION OF EXTENSION BLIND SPOTS

3. Resolving the Disappearing Overdue Surcharge on Extension

* Problem Statement: A critical logic blind spot remains when executing room extensions. When a guest passes their contracted 12:00 checkout deadline and requests an extension later (e.g., at 14:00), the 2 hours of accumulated late checkout penalty vanishes from the system calculations upon pressing the extension button.
* Resolution Plan: Direct the Codex agent to run a comprehensive trace on the segment splitting logic. When an extension occurs, the historical segment's NgayCheckOut contract time must remain frozen at its original contract deadline (e.g., 12:00), while the reference time evaluating that segment's end must anchor to the creation time of the next extension segment or actual mutation point (e.g., 14:00). Codex must ensure no hidden code paths or structural state overrides purge historical overdue hours during sequential room state switches.
* Output Requirements: The 2-hour late penalty must remain accurately preserved and calculated as an overdue charge inside the historical segment list row, entirely decoupled from the newly added extension timeline.

PHASE 3: ACCOMMODATION NIGHT COUNT ALIGNMENT

4. Correction of Night Calculation Metrics (Contract vs. Actual)

* Problem Statement: The night count calculation (SoDem) on the checkout invoice is broken. It currently relies strictly on the original contract checkout date rather than the actual checkout event. For instance, if a contract spans from Check-In on the 23rd to Contract Checkout on the 25th (2 nights), but the guest executes an early actual checkout on the 24th, the invoice incorrectly demands payment for 2 nights instead of the actual 1 night stayed (23rd to 24th).
* Resolution Plan: Refactor the NormalizeStoredNightCount and GetStoredRoomTotal helper methods inside HoaDonService. Modify the counting algorithm so that for the currently active room segment, the billing night metric is dynamically computed using the actual real-world checkout moment (DateTime.Now or NgayThanhToan) rather than the contract deadline, rounding down or up appropriately based on standard hotel occupancy tracking rules.
* Output Requirements: If a guest cuts their trip short and checks out a day early, the room total (TongTienPhong) and night breakdown display (SoDemText) must automatically adjust to reflect only the nights spent between the check-in date and the actual checkout moment.