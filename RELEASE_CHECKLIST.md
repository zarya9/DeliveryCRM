# Release Checklist

## 1. Pre-deploy validation
- Run `dotnet build APIDeliveryCRM.sln`.
- Ensure EF migrations are present for all new modules (billing, notifications, vehicles, templates, scheduled reports).
- Verify `appsettings` values for billing provider, webhook secret, and allowed webhook IPs.
- Confirm JWT/auth cookie settings match environment domain and HTTPS mode.

## 2. Smoke tests by role
- Manager: dashboard, orders, leads, analytics, billing, notifications.
- Logistician: courier list, courier card, route planning, distribution.
- Customer: create order, list orders, order tracking.
- Admin: employees, audit logs, access control.

## 3. Billing and webhook checks
- Create checkout session and verify redirect to payment page.
- Complete a successful payment and verify invoice status becomes `paid`.
- Send duplicate webhook event and verify idempotency (no duplicate processing).
- Validate webhook security checks (secret header and allowed IP behavior).

## 4. Realtime checks
- Open chat/notifications in two sessions and verify new notifications arrive instantly.
- Verify courier availability list auto-refreshes and reflects online/offline changes.
- Confirm no JS console errors during SignalR reconnect.

## 5. Data and UX checks
- Verify status badges are consistent across orders, leads, billing, notifications.
- Validate SLA risk/overdue highlighting in manager pages.
- Check 404 page (`/missing-route`) displays custom "МИМО" screen.

## 6. Post-deploy monitoring
- Track API 5xx rate and webhook failure count.
- Monitor background worker execution for scheduled reports.
- Watch payment transaction error logs for the first 24h.
