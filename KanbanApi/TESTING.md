# Manual Smoke Test Plan

This document outlines the manual verification steps required to ensure the stability of the Kanban application.

## 1. Authentication
- [x] **Registration:** Sign up with a new account.
- [x] **Login:** Log in with the registered credentials.
- [x] **Logout:** Verify that logging out clears local storage and redirects to the login page.

## 2. Dashboard & Navigation
- [x] **Create Board:** Create a new board and ensure it appears on the dashboard.
- [x] **Navigation:** Click on the board title to navigate to `/board/{id}`.

## 3. Board Operations
- [x] **Columns:** Add new columns (e.g., "To Do", "In Progress").
- [x] **Cards:** Create new cards within columns.
- [x] **Drag & Drop:** Move cards between different columns.
- [x] **Persistence:** Refresh the page (F5) and ensure the card position and new columns are saved.

## 4. Collaboration
- [x] **Invitation:** Open the "Share/Invite" modal, enter a valid User ID, and submit.
- [x] **Verification:** Log in as the invited user and confirm the board appears on their dashboard.