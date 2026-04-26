# Chat Smoke Checklist

Use this checklist for final manual verification of chat flow.

## Preconditions

- Apply latest migrations (`dotnet ef database update` in `APIDeliveryCRM`).
- Start backend and frontend.
- Login with at least 2 users from same company (two browser sessions).

## Core flow

- Send plain text message.
- Edit own message.
- Delete own message for everyone.
- Hide own message locally ("Скрыть у меня").

## Attachments

- Upload non-image file (e.g. `.pdf`) and send.
- Verify icon + file name + open link.
- Upload image (`.jpg`/`.png`) and send.
- Verify inline preview in message card.

## Quick replies and templates

- Switch categories:
  - Greeting
  - Clarification
  - SLA/Delay
  - Closing
- Click quick reply and verify insertion into input.
- Create personal template in current category.
- Search template by title/content.
- Edit template and verify changes.
- Delete template and verify it disappears.

## Realtime behavior

- With 2 users in same room:
  - new message appears without refresh,
  - edit event appears without refresh,
  - delete event appears without refresh.

## API security/scope checks

- Verify template endpoints return only current company + current user templates.
- Verify unauthorized request to chat/file endpoints is rejected.

## Done criteria

- All checks pass.
- No frontend runtime errors in browser console.
- No 5xx errors in backend logs during smoke.
