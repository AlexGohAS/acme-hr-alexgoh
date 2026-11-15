# Feature spec: leave requests with half-day leave (Lab 1)

## Endpoints
- `POST /leave` — body: `startDate`, `endDate`, `halfDay` (`None` | `AM` | `PM`), `reason`.
  The employee is the signed-in user (`User.EmployeeId()`). An `employeeId` in the body is ignored.
- `POST /leave/{id}/approve` and `POST /leave/{id}/reject`.

## Rules
1. `endDate` must not be before `startDate`. Half day requires `startDate == endDate` and a working day.
2. Days are working days (Mon–Fri); a half day is 0.5.
3. Reject if the employee's available balance is less than the days requested.
4. Reject if it overlaps another Pending or Approved request of the same employee
   (two half days on the same date, one AM and one PM, do not overlap).
5. Up to 3 working days: status `Approved` immediately. More than 3: status `Pending` (manager approval).
6. Only `Pending` requests can be approved or rejected.
7. Only the requester's manager or HR may approve or reject, and never their own request
   (`AccessService.CanDecideLeaveAsync`). Anyone else gets 403.
8. Status codes: a broken rule (validation, balance, overlap, not `Pending`) → 400 ProblemDetails with a clear message;
   unknown leave request → 404; not allowed (rule 7) → 403.
9. A full-day request with no working days (for example Saturday to Sunday) → 400.
10. `startDate` and `endDate` are required: a missing date → 400. Never default to 0001-01-01.
11. Past start dates are allowed (an employee can record leave already taken).

## Also
- Fix the known bug: leave spanning a weekend counts Saturday and Sunday (see the failing test).
- API: each leave request in responses includes `days`, its working days (0.5 for a half day).
- Web: the Leave page shows each request's working days (the `days` from the API, never counted in the browser), its status, and the signed-in employee's available balance, and lets the user create a request.
