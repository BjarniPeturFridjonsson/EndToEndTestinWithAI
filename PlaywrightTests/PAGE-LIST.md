# Page Test Work List

Structured by access level. Use this as the task list when generating tests with Playwright MCP.
Check off pages as tests are written. Pages marked ⚠️ need special handling noted below.

Auth state to use per area is in `Setup/TestSettings.cs`.

---

## 🔑 No auth required (Public / CmsWeb)
*Auth state: none — navigate directly*

- [ ] `CmsWeb/Pages/Login` — login page ⚠️ smoke test only, no auth needed
- [ ] `CmsWeb/Pages/Page` —  CMS page ⚠️ content-driven, check title only
- [ ] `CmsWeb/Pages/Post` — CMS post ⚠️ content-driven, check title only
- [ ] `CmsWeb/Pages/Archive` — CMS archive ⚠️ content-driven
- [ ] `CmsWeb/Pages/Profile` — user profile

---

## 👤 Member (any authenticated user)
*Auth state: `AuthStateMember` — TestUserRegularMember@localtest.com*
*Base class: `UserPagesTestBase`*

### UserPages — Dashboard & Profile
- [ ] `UserPages/Index` — main member dashboard ⭐ key page
- [ ] `UserPages/Index2`
- [ ] `UserPages/MembersDetails` — member detail view
- [ ] `UserPages/MemberEdit` — edit own profile
- [ ] `UserPages/TransIndex` — transaction history

### UserPages — Flights
- [ ] `UserPages/FlightsIndex` — flight list ⭐ key page, part of E2E test
- [ ] `UserPages/FlightsDetails` — flight detail
- [ ] `UserPages/EditFlightInfo` — edit flight info
- [ ] `UserPages/PilotCareer/PilotInfoIndex` — pilot career info

### UserPages — Bookings
- [ ] `UserPages/Bookings/BookingIndex` — booking overview ⭐ key page
- [ ] `UserPages/Bookings/Index` — booking list
- [ ] `UserPages/Bookings/Listing` — booking listing
- [ ] `UserPages/Bookings/Overview` — bookings overview
- [ ] `UserPages/Bookings/Create` — create booking
- [ ] `UserPages/Bookings/Edit` — edit booking
- [ ] `UserPages/Bookings/Delete` — delete booking
- [ ] `UserPages/Bookings/CreateOld` ⚠️ legacy page, may be deprecated

### UserPages — Exam
- [ ] `UserPages/Exam/Index` — exam menu
- [ ] `UserPages/Exam/Quiz` — take a quiz
- [ ] `UserPages/Exam/Test` — exam test
- [ ] `UserPages/Exam/Results` — exam results
- [ ] `UserPages/Exam/History` — exam history

---

## 🛠️ Admin
*Auth state: `AuthStateAdmin` — TestUserAdminMember@localtest.com*
*Base class: `AdminTestBase`*
*Roles allowed: AdminRole, TrainingRole, SuperRole, DisplayRole*

### Members
- [ ] `Admin/Members/Index` — member list ⭐ key page
- [ ] `Admin/Members/Details` — member detail
- [ ] `Admin/Members/Create` — create member
- [ ] `Admin/Members/Edit` — edit member

### Flights25 (current flight system) ⭐ E2E test target
- [ ] `Admin/Flights25/Index` — flight list ⭐ key page
- [ ] `Admin/Flights25/Create` — register flight ⭐ E2E step 1
- [ ] `Admin/Flights25/Details` — flight detail
- [ ] `Admin/Flights25/Edit` — edit flight
- [ ] `Admin/Flights25/Delete` — delete flight ⭐ E2E step 3
- [ ] `Admin/Flights25/Charges` — flight charges ⭐ E2E verification
- [ ] `Admin/Flights25/Payments` — payments

### Flights20 (legacy flight system)
- [ ] `Admin/Flights20/Index`
- [ ] `Admin/Flights20/Create`
- [ ] `Admin/Flights20/Details`
- [ ] `Admin/Flights20/Delete`
- [ ] `Admin/Flights20/Charges`
- [ ] `Admin/Flights20/Payments`

### FlightsOps (flight operations)
- [ ] `Admin/FlightsOps/Index`
- [ ] `Admin/FlightsOps/Create`
- [ ] `Admin/FlightsOps/Details`
- [ ] `Admin/FlightsOps/Edit`
- [ ] `Admin/FlightsOps/Delete`

### Contracts
- [ ] `Admin/Contracts/Index`
- [ ] `Admin/Contracts/Create`
- [ ] `Admin/Contracts/Details`
- [ ] `Admin/Contracts/Edit`
- [ ] `Admin/Contracts/Delete`
- [ ] `Admin/Contracts/FlightsList`
- [ ] `Admin/Contracts/Summaries`

---

## 📋 Training
*Auth state: `AuthStateTraining` — TestUserTrainingMember@localtest.com*
*Base class: `TrainingTestBase`*
*Roles allowed: TrainingRole, SuperRole*

### Training Admin — ExamAdmin
- [ ] `Training/Index` — training dashboard ⭐ key page
- [ ] `Training/ExamAdmin/Index` — exam admin
- [ ] `Training/ExamAdmin/QuizCategories` — quiz category list
- [ ] `Training/ExamAdmin/CategoryEdit` — edit category
- [ ] `Training/ExamAdmin/QuizQuestions` — question list
- [ ] `Training/ExamAdmin/QuizQuestionEdit` — edit question
- [ ] `Training/ExamAdmin/WrittenExams` — written exam list
- [ ] `Training/ExamAdmin/WrittenExamDetails` — exam detail
- [ ] `Training/ExamAdmin/WrittenExamEdit` — edit exam
- [ ] `Training/ExamAdmin/StudentProgress` — student progress

### Training Admin — StudentTasks
- [ ] `Training/StudentTasks/StudentsIndex` — student list
- [ ] `Training/StudentTasks/Index` — task list
- [ ] `Training/StudentTasks/Create` — create task
- [ ] `Training/StudentTasks/TaskEdit` — edit task
- [ ] `Training/StudentTasks/DetailEdit` — edit task detail

---

## 🔒 Super
*Auth state: `AuthStateSuper` — TestUserSuperMember@localtest.com*
*Base class: `SuperTestBase`*
*Role: SuperRole*

### Capabilities ✅ smoke test already written
- [x] `Super/Capabilities/Index`
- [ ] `Super/Capabilities/Details`
- [ ] `Super/Capabilities/Create`
- [ ] `Super/Capabilities/Edit`
- [ ] `Super/Capabilities/EditRow`
- [ ] `Super/Capabilities/CapabilityRows`
- [ ] `Super/Capabilities/Delete`

### Master Files & Reference Data
- [ ] `Super/MasterFiles/Index` ⭐ key page
- [ ] `Super/Enquiries/Index`

### InfoTypes
- [ ] `Super/InfoTypes/Index`
- [ ] `Super/InfoTypes/Details`
- [ ] `Super/InfoTypes/Edit`
- [ ] `Super/InfoTypes/EditRow`
- [ ] `Super/InfoTypes/InfoRows`
- [ ] `Super/InfoTypes/Delete`

### Items
- [ ] `Super/Items/Index`
- [ ] `Super/Items/Details`
- [ ] `Super/Items/Create`
- [ ] `Super/Items/Edit`
- [ ] `Super/Items/Delete`

### Sites
- [ ] `Super/Sites/Index`
- [ ] `Super/Sites/Details`
- [ ] `Super/Sites/Create`
- [ ] `Super/Sites/Edit`
- [ ] `Super/Sites/Delete`

### FlightTypes
- [ ] `Super/FlightTypes/Index`
- [ ] `Super/FlightTypes/Details`
- [ ] `Super/FlightTypes/Create`
- [ ] `Super/FlightTypes/Edit`
- [ ] `Super/FlightTypes/Delete`

### Tariffs
- [ ] `Super/Tariffs/Index`
- [ ] `Super/Tariffs/Details`
- [ ] `Super/Tariffs/Create`
- [ ] `Super/Tariffs/Edit`
- [ ] `Super/Tariffs/Delete`

### TrainingTasks
- [ ] `Super/TrainingTasks/Index`
- [ ] `Super/TrainingTasks/Create`
- [ ] `Super/TrainingTasks/Edit`
- [ ] `Super/TrainingTasks/Delete`
- [ ] `Super/TrainingTasks/DetailsIndex`
- [ ] `Super/TrainingTasks/DetailsCreate`
- [ ] `Super/TrainingTasks/DetailsEdit`
- [ ] `Super/TrainingTasks/DetailsDelete`

### WebCare ⚠️ requires SuperAdmin role — DEFERRED
- [ ] `Super/WebCare/Index` ⚠️
- [ ] `Super/WebCare/Dashboard` ⚠️
- [ ] `Super/WebCare/CurrentUsers` ⚠️
- [ ] `Super/WebCare/Logs` ⚠️
- [ ] `Super/WebCare/DayDetail` ⚠️

---

## 🚫 Authorization tests (wrong-role blocked)
*Separate test class per area using wrong auth state*
*See TEST-PLAN.md for pattern*

- [ ] Admin pages blocked for Member
- [ ] Training pages blocked for Member
- [ ] Super pages blocked for Member
- [ ] Super pages blocked for Admin

---

## ⭐ E2E: Flight Registration
*Single context, Admin auth (TestUserAdminMember is also pilot)*
*See TEST-PLAN.md for full design — needs Flights25 form investigation first*

- [ ] Register flight → verify on UserPages → delete → verify removed
