# MASTER REDESIGN IMPLEMENTATION PLAN — ONLINE FOOD ORDERING WEB APP

**Target Solution:** Online Food Ordering Web App  
**Architecture:** ASP.NET Core MVC (.NET 10.0), Entity Framework Core 10.0.1, SQLite, Razor Views, Session Management, Stripe Checkout  
**Primary Goal:** Transform the existing baseline application into a premium, responsive, appetizing, commercial-grade food ordering platform with an animated cart experience, genuine notification workflows, robust security, and seamless deployment compatibility on ASP.NET Monster/cloud hosting.

---

## 1. Executive Summary & Repository Audit

### 1.1 Technology Stack & Existing Architecture
- **Framework & Runtime:** ASP.NET Core MVC on `.NET 10.0` (`net10.0` specified in `OnlineFoodOrdering.csproj`).
- **Data Persistence:** Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite` v10.0.1). Connection string points to local file `Data Source=OnlineFoodOrdering.db`.
- **Session & State:** In-memory distributed session (`builder.Services.AddSession()`, `app.UseSession()`). Cart is stored as a serialized JSON string under session key `"Cart"`. Authentication status is tracked via session keys `"UserId"`, `"UserName"`, `"IsAdmin"`.
- **Payment Gateway:** Stripe (`Stripe.net` v50.1.0) using Stripe Checkout Sessions (`SessionService`).
- **Frontend Assets:** Bootstrap 5.x CSS & JS bundle, jQuery 3.x, jQuery Validation + Unobtrusive Validation, custom `site.css` and `site.js`.

### 1.2 Existing Entities & Schema State
1. **`User` (`Models/User.cs`):**
   - Fields: `UserId` (PK), `FullName` (Required), `Email` (Required, EmailAddress), `Password` (Required, plain-text currently), `PhoneNumber` (Required, Phone), `IsAdmin` (bool).
2. **`FoodItem` (`Models/FoodItem.cs`):**
   - Fields: `FoodItemId` (PK), `Name` (Required), `Price` (Required decimal), `ImagePath` (string).
   - *Note:* Lacks description, category, calories, preparation time, and availability flags.
3. **`Order` (`Models/Order.cs`):**
   - Fields: `OrderId` (PK), `UserId` (FK -> `User`), `CustomerName`, `PhoneNumber`, `Address`, `PaymentMethod`, `Status` (Default: `"Processing"`), `OrderDate` (DateTime), `OrderItems` (1-to-many navigation).
4. **`OrderItem` (`Models/OrderItem.cs`):**
   - Fields: `OrderItemId` (PK), `OrderId` (FK -> `Order`), `FoodItemId` (FK -> `FoodItem`), `Quantity` (int).
5. **`Notification` (`Models/Notification.cs`):**
   - Fields: `Id` (PK), `UserId` (int), `Message` (string), `IsRead` (bool), `CreatedAt` (DateTime).
   - *Audit finding:* No navigation property back to `User` or `Order`. Currently, notifications are only created when an admin changes an order status via `AdminController.UpdateOrderStatus`.
6. **`CartItem` (`Models/CartItem.cs`):**
   - Memory POCO: `FoodItemId`, `Name`, `Price`, `Quantity`.
   - *Audit finding:* Does not carry `ImagePath`, requiring cart rendering to use generic placeholders or lookup images.

### 1.3 Existing Routing & Feature Inventory
| Route / Action | Controller | Method | Current Status | Redesign Scope |
| :--- | :--- | :--- | :--- | :--- |
| `/` or `/Main/Index` | `MainController` | GET | Displays basic grid of food cards, simple search | Hero banner, categories, search + filter bar, appetizing cards, badge counter, quick add-to-cart |
| `/Main/AddToCart/{id}` | `MainController` | GET | Synchronous GET redirect to Index; increments item | Convert to AJAX JSON endpoint (`POST /Main/AddToCartAjax`), keep GET fallback; triggers drawer/toast |
| `/Main/Cart` | `MainController` | GET | Basic HTML table, remove button | Polished cart page + interactive quantities (+/-), slide-in quick drawer |
| `/Main/UpdateQuantityAjax` | `MainController` | POST | **Missing** (Quantity cannot be adjusted in cart) | **Must implement:** Real-time quantity increment/decrement with subtotal recalculation |
| `/Main/RemoveFromCart/{id}` | `MainController` | GET | Synchronous GET redirect to Cart | Support both AJAX removal and full-page fallback |
| `/Main/Checkout` | `MainController` | GET/POST | Simple forms for COD and Stripe Checkout redirect | Multi-column checkout summary, sticky order breakdown, clean payment radio toggle |
| `/Main/StripeSuccess` | `MainController` | GET | Creates order from session, redirects to MyOrders | Verified payment confirmation view with receipt preview and order tracking prompt |
| `/Main/StripeCancel` | `MainController` | GET | Sets TempData and redirects to Cart | Elegant cancellation screen with retry options |
| `/Main/MyOrders` | `MainController` | GET | Basic table of user orders | Order card timeline with live status pills (Processing -> Confirmed -> Delivered) |
| `/Main/MarkNotificationRead/{id}`| `MainController` | GET | Marks single notification read, redirects to Index | Convert to AJAX endpoint, return updated unread count |
| `/Main/MarkAllNotificationsRead` | `MainController` | POST | **Missing** | **Must implement:** Bulk mark all read via AJAX |
| `/Main/GetNotificationsAjax` | `MainController` | GET | **Missing** | **Must implement:** Dynamic poll/fetch for notification dropdown |
| `/Account/Login` | `AccountController`| GET/POST | Minimal card login with plain session set | Split-screen culinary photography design, form validation, error banners |
| `/Account/Register` | `AccountController`| GET/POST | Minimal register form | Polished customer registration with matching aesthetic and input validation |
| `/Account/Logout` | `AccountController`| GET | Clears session, redirects to Index | Header action dropdown, secure session clear |
| `/Admin/Index` | `AdminController` | GET | Two simple cards (Orders, AddFood) | Metric summary dashboard (Total revenue, pending orders, item count, quick links) |
| `/Admin/Orders` | `AdminController` | GET | Table with status filter & OrderId sort | Filter tabs, customer badge, line item detail modal/expansion, responsive status updater |
| `/Admin/UpdateOrderStatus` | `AdminController` | POST | Form post updates status and writes notification | Preserved with toast feedback and order status notifications |
| `/Admin/FoodList` | `AdminController` | GET | Simple table with images and actions | Responsive data table with search, badge pricing, modal confirmations for delete |
| `/Admin/AddFood` | `AdminController` | GET/POST | Upload image to `wwwroot/images` | Drag-and-drop / preview file input, validation, default image fallback |
| `/Admin/EditFood/{id}` | `AdminController` | GET/POST | Basic edit form | Live preview of image change, validation, price formatting |
| `/Admin/DeleteFood/{id}` | `AdminController` | GET | Deletes item without order foreign-key validation | Safe deletion with confirmation modal and cascading error protection |

---

## 2. Brand Identity & Visual Design System

### 2.1 Color Palette
- **Canvas / Backgrounds:**
  - `Surface Ivory`: `#FBF9F5` (Primary body background)
  - `Warm Cream`: `#F5EFEB` (Secondary containers, card backgrounds)
  - `Pure White`: `#FFFFFF` (Elevated cards, modals, popovers)
- **Primary Actions:**
  - `Chili Red`: `#D9381E` (Primary brand color, checkout buttons, active badges)
  - `Chili Red Hover`: `#BF2E17`
  - `Chili Red Subtle`: `#FDF2F0` (Badge backgrounds, active tab highlights)
- **Secondary & Accent:**
  - `Basil Green`: `#2E7D32` (Success states, payment confirmed badges, vegan/fresh accents)
  - `Basil Green Subtle`: `#E8F5E9`
  - `Saffron Amber`: `#E67E22` (Processing states, rating stars, alert highlights)
- **Typography & Neutrals:**
  - `Espresso Black`: `#1F1916` (Primary headings, high-contrast labels)
  - `Charcoal Body`: `#3D3531` (Paragraph text, descriptions)
  - `Muted Truffle`: `#7B726C` (Timestamps, secondary metadata, borders)
  - `Subtle Border`: `#E7E0D8` (Cards, divider lines, inputs)

### 2.2 Typography
- **Heading Font:** `'Playfair Display', Georgia, serif` or `'Plus Jakarta Sans', system-ui, sans-serif` (Premium, warm hospitality feel for titles).
- **Body & UI Font:** `'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif` (Optimal readability for prices, navigation, forms).
- **Numeric Font:** Tabular figures enabled (`font-variant-numeric: tabular-nums`) for prices, order numbers, and cart badges.

### 2.3 Component Design Principles
- **Atmosphere:** Warm, appetizing, tactile, and professional. Eliminates cold default Bootstrap styling.
- **Card Styling:** Soft borders (`1px solid #E7E0D8`), subtle ambient shadows (`0 8px 24px rgba(31, 25, 22, 0.05)`), rounded radii (`12px` to `16px`).
- **Elevated Food Imagery:** Images display with subtle 1.03x hover scale, gentle overflow hidden, aspect ratio `4:3` or `16:9` with responsive `object-fit: cover`.
- **Transitions:** Smooth bezier easing (`transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1)`).

---

## 3. Animated Shopping Cart Architecture

### 3.1 Components & UX Flow
1. **Header Cart Icon & Live Badge:**
   - Positioned in primary navigation with SVG shopping bag icon.
   - Badge displays total item count (or sum of quantities).
   - Dynamic pop animation (`@keyframes cartBadgePop { 0% { transform: scale(1); } 50% { transform: scale(1.35); } 100% { transform: scale(1); } }`) triggered on any quantity change.
2. **Slide-in Cart Drawer (`#cartDrawer`):**
   - Off-canvas drawer sliding from the right on desktop and mobile.
   - Triggers when an item is added or when clicking the cart icon.
   - Contains:
     - Header with total item count and close button (`×`).
     - Scrollable list of current items with thumbnail, title, price, quantity stepper (`-`, `qty`, `+`), and delete icon.
     - Sticky footer displaying subtotal, delivery note ("Free delivery on orders over $30"), and primary buttons: `"View Full Cart"` and `"Proceed to Checkout"`.
     - Polished empty state with food illustration and `"Explore Menu"` CTA.
3. **Interactive Add-to-Cart Feedback:**
   - Clicking `"Add to Cart"` changes button state to a brief loading spinner, followed by a checkmark badge (`"Added!"`) for 1.2s before resetting.
   - Sends asynchronous request to `POST /Main/AddToCartAjax`.
   - On success, updates badge count, replaces drawer contents, and pops drawer open without reloading the page.
   - On failure, displays an accessible error toast.
4. **Authoritative Server-Side Math:**
   - Client-side code never computes authoritative prices. Every AJAX quantity adjustment queries `/Main/UpdateCartItemQuantityAjax` which re-validates against database prices and returns updated line totals, item counts, and grand total.

---

## 4. Notification System Architecture

### 4.1 Functional Requirements
- Unread count badge on the navigation bell icon with subtle pulse animation upon arrival of new alerts.
- Interactive dropdown panel displaying:
  - Header with unread count and `"Mark all as read"` button.
  - Scrollable notification list showing icon (order placed, confirmed, delivered), message, relative time (`"5m ago"`, `"Yesterday"`), and unread accent dot.
  - Direct links to `/Main/MyOrders` or specific order tracking.
  - Empty state: `"All caught up! No new notifications"`.
- Real-time or short-poll mechanism: Periodic lightweight client polling (e.g. every 30-45s) via `GET /Main/GetUnreadNotificationsAjax` to update badge without web sockets or expensive infrastructure.

### 4.2 Database & Backend Additions
- **Model Enhancements (`Notification.cs`):**
  - Add optional foreign key: `public int? OrderId { get; set; }`
  - Add notification category: `public string Type { get; set; } = "Order"` (`"Order"`, `"Payment"`, `"Admin"`)
- **Controller Endpoints (`MainController.cs`):**
  - `GET /Main/GetNotificationsAjax`: Returns `{ unreadCount = N, notifications = [...] }` (JSON).
  - `POST /Main/MarkNotificationReadAjax/{id}`: Marks specific notification read and returns success.
  - `POST /Main/MarkAllNotificationsReadAjax`: Marks all notifications for current user as read.
- **Event Triggers:**
  1. *Order Created (COD):* User receives "Order #X placed successfully. Status: Processing." Admin receives alert on dashboard.
  2. *Order Paid (Stripe):* User receives "Payment verified! Order #X is confirmed."
  3. *Admin Status Update:* User receives "Your order #X has been updated to [Delivered/Confirmed]."

---

## 5. Page-by-Page Redesign Plan

### 5.1 Shared Navigation & Layout (`Views/Shared/_Layout.cshtml`)
- **Top Announcement Bar:** Subtle warm bar for announcements ("Fresh ingredients prepared daily • Quick delivery to your door").
- **Navbar:** Sticky header with backdrop blur (`backdrop-filter: blur(12px)`), logo brand with food icon, active category links, search input with clear button, user menu, notification bell dropdown, and animated cart drawer trigger.
- **Flash Messages:** Modern toast notification stack positioned fixed top-right, replacing raw alert bars.
- **Footer:** Implements exact required attribution and links:
  - Attribution: `© 2026 Rifat. All rights reserved.` (dynamic year with fallback).
  - Link 1: **GitHub Repository** -> `https://github.com/Rifat-Bin-Alam/Online-Food-Ordering-WebApp` (`target="_blank" rel="noopener noreferrer"`).
  - Link 2: **Developer Portfolio** -> `https://rifat-bin-alam.vercel.app/` (`target="_blank" rel="noopener noreferrer"`).
  - Quick navigation links, operating hours, payment method badges (Cash, Stripe/Cards).

### 5.2 Homepage & Food Menu (`Views/Main/Index.cshtml`)
- **Hero Section:** High-impact food visual, compelling tagline ("Artisanal Flavors Delivered to Your Doorstep"), search input with instant filter, and fast scroll CTAs.
- **Category Filter Pills:** All, Burgers, Pizza, Pasta, Desserts, Sandwiches (client-side dynamic category filtering based on food tags/names).
- **Food Grid:** Modern grid (`grid-template-columns: repeat(auto-fill, minmax(260px, 1fr))`).
  - Food card contains: appetizing image with aspect ratio clamp, price pill badge, title, subtle description placeholder, and vibrant `"Add to Cart"` action with animated states.
- **Features Bar:** 3 value propositions (Fast Delivery, Fresh Quality, Secure Checkout).

### 5.3 Cart View (`Views/Main/Cart.cshtml`)
- Two-column responsive layout:
  - Left column: Detailed item cards/table with item thumbnail, title, unit price, quantity increment/decrement controls, total price, and removal confirmation.
  - Right column: Sticky order summary card showing Subtotal, Delivery Fee ($0 or calculated), Estimated Tax, Total, Promo code input (visual), and `"Proceed to Checkout"` CTA.
- Empty cart state with illustration and `"Return to Menu"` button.

### 5.4 Checkout View (`Views/Main/Checkout.cshtml`)
- Streamlined 2-column layout:
  - Left column: Contact Details (Name, Phone), Delivery Address, and Payment Method selection.
  - Payment method card selector: Custom radio cards for **Cash on Delivery** and **Stripe Card Payment**.
  - Right column: Order item summary with thumbnail badges and order total.
- Form validation indicators for required phone numbers and street addresses.
- Clear visual indicator for Stripe test card (`4242 4242 4242 4242`).

### 5.5 Order History (`Views/Main/MyOrders.cshtml`)
- Order cards with status pipeline visual:
  - `Processing` (Amber) -> `Confirmed` (Primary Chili) -> `Delivered` (Basil Green).
- Details include Order ID, date/time formatted nicely, itemized list with quantities and prices, payment method badge, and delivery address.
- Empty state encouraging user to place their first order.

### 5.6 Authentication (`Views/Account/Login.cshtml` & `Register.cshtml`)
- Warm editorial split layout:
  - Left side: High-resolution food imagery with inspirational hospitality quote.
  - Right side: Clean card with tabbed switch between Login and Register.
- Floating input labels, clear error validation banners, password visibility toggle.

### 5.7 Admin Dashboard & Management (`Views/Admin/*`)
- **Dashboard (`Index.cshtml`):** Stat cards (Total Orders, Pending Orders, Delivered Orders, Menu Item Count), quick action cards ("Add New Item", "Review Pending Orders").
- **Orders Management (`Orders.cshtml`):**
  - Status filter pill tabs (`All`, `Processing`, `Confirmed`, `Delivered`).
  - Order ID sorting buttons.
  - Responsive table featuring customer info, expandable line items, order date, current status badge, and an inline status update form with immediate visual feedback.
- **Food List (`FoodList.cshtml`):** Image thumbnail preview, food name, price, edit and delete actions with confirmation modals.
- **Add / Edit Food (`AddFood.cshtml` & `EditFood.cshtml`):** Card forms with live image file preview, file drag area, and validation feedback.

---

## 6. Comprehensive Interactive Element Inventory & Button Checklist

| Page / Component | Element | Type | Action / Destination | Error & State Handling |
| :--- | :--- | :--- | :--- | :--- |
| **Navbar** | Brand Logo | Link | Navigates to `/Main/Index` | Always active |
| **Navbar** | Search Input & Button | Form | Submits GET query to `/Main/Index?search=...` | Graceful empty search handling |
| **Navbar** | Notification Bell | Button/Dropdown | Opens notification dropdown panel | Shows badge count; shows empty message if 0 |
| **Navbar** | "Mark all read" | Button (AJAX) | Calls `POST /Main/MarkAllNotificationsReadAjax` | Disables while processing; updates badge to 0 |
| **Navbar** | Notification Item | Link | Marks notification read & opens `/Main/MyOrders` | Closes dropdown, updates read state |
| **Navbar** | Cart Bag Icon | Button | Opens Cart Drawer (`#cartDrawer`) | Accessible aria controls |
| **Navbar** | User Login Link | Link | Navigates to `/Account/Login` | Present when logged out |
| **Navbar** | User Logout Link | Link | Navigates to `/Account/Logout` | Clears session securely |
| **Navbar** | Admin Dashboard Link| Link | Navigates to `/Admin/Index` | Visible only when `IsAdmin == 1` |
| **Hero / Home**| "Explore Menu" CTA | Link | Smooth scrolls to `#menuSection` | Smooth scroll behavior |
| **Home Menu** | Category Pills | Buttons | Filters menu cards in-place without page reload | Highlights active pill |
| **Food Card** | "Add to Cart" | Button (AJAX) | Sends POST to `/Main/AddToCartAjax/{id}` | Shows loading spinner -> checkmark -> updates drawer |
| **Cart Drawer**| Close Button (`×`) | Button | Dismisses drawer overlay | Keyboard ESC support |
| **Cart Drawer**| `+` / `-` Quantity | Button (AJAX) | Updates quantity via AJAX | Prevents decrement below 1; recalculates totals |
| **Cart Drawer**| Remove Item | Button (AJAX) | Removes item from session cart via AJAX | Animates card removal, updates badge |
| **Cart Drawer**| "Proceed to Checkout"| Link | Navigates to `/Main/Checkout` | Disabled if cart is empty |
| **Cart Page** | `+` / `-` Quantity | Button (AJAX/POST)| Adjusts quantity and line total | Real-time calculation |
| **Cart Page** | "Remove" | Button | Removes item from cart | Confirmation toast |
| **Cart Page** | "Continue Shopping"| Link | Navigates to `/Main/Index` | Retains cart contents |
| **Cart Page** | "Proceed to Checkout"| Link | Navigates to `/Main/Checkout` | Redirects to Login if unauthenticated |
| **Checkout** | Payment Radios | Radio | Toggles COD vs Stripe Card options | Switches form action / Stripe Elements display |
| **Checkout** | "Place Order" (COD) | Submit | Submits form to `POST /Main/Checkout` | Validates required fields; prevents double-submit |
| **Checkout** | "Pay with Stripe" | Submit | Initiates Stripe Checkout session | Disables button and displays redirect spinner |
| **Orders Page**| Status Filter Tabs | Links | Filters table by query `?status=...` | Preserves sorting state |
| **Orders Page**| Order ID Sort (↑/↓) | Links | Toggles sort order `?sortOrder=asc\|desc` | Preserves filter state |
| **Orders Page**| Status Update Dropdown| Form/Submit| Updates order status via `POST /Admin/UpdateOrderStatus` | Updates DB and notifies user |
| **Food List** | "Add New Food Item"| Link | Navigates to `/Admin/AddFood` | Admin access only |
| **Food List** | "Edit" Button | Link | Navigates to `/Admin/EditFood/{id}` | Admin access only |
| **Food List** | "Delete" Button | Form/Modal | Deletes item with confirmation modal | Prevents accidental deletion |
| **Food Form** | File Upload Input | File Input | Selects image for upload | Displays live preview before saving |
| **Footer** | GitHub Link | External Link | Opens GitHub repository in new tab | `rel="noopener noreferrer"` |
| **Footer** | Portfolio Link | External Link | Opens Portfolio in new tab | `rel="noopener noreferrer"` |

---

## 7. Demo Accounts & Authentication Verification Procedure

### 7.1 Target Accounts & Current Findings
- **Customer Account:**
  - Email: `test@gmail.com`
  - Password: `1234`
  - Role: Regular customer (`IsAdmin = false`)
- **Administrator Account:**
  - Email: `rifat@gmail.com`
  - Password: `11223344`
  - Role: Store Administrator (`IsAdmin = true`)

### 7.2 Database Inspection & Seeding Strategy
- **Current Auth Mechanism:** `AccountController.Login` performs direct query:
  `_context.Users.FirstOrDefault(u => u.Email == email && u.Password == password)`
  Passwords in the existing implementation are stored in plain text.
- **Account Verification & Idempotent Seeder:**
  To guarantee demo credentials function reliably across environments without manual database manipulation, we will implement an idempotent initialization routine in `Program.cs` / `DbInitializer.cs`:
  ```csharp
  public static class DbInitializer
  {
      public static void SeedUsers(ApplicationDbContext context)
      {
          context.Database.EnsureCreated();

          // Ensure Customer Account
          if (!context.Users.Any(u => u.Email == "test@gmail.com"))
          {
              context.Users.Add(new User
              {
                  FullName = "Test Customer",
                  Email = "test@gmail.com",
                  Password = "1234",
                  PhoneNumber = "01700000000",
                  IsAdmin = false
              });
          }

          // Ensure Admin Account
          if (!context.Users.Any(u => u.Email == "rifat@gmail.com"))
          {
              context.Users.Add(new User
              {
                  FullName = "Rifat (Admin)",
                  Email = "rifat@gmail.com",
                  Password = "11223344",
                  PhoneNumber = "01800000000",
                  IsAdmin = true
              });
          }

          context.SaveChanges();
      }
  }
  ```
- **Security & Disclosure Rule:** Demo credentials will **not** be hardcoded into client-facing HTML or login views, but will be safely documented and seeded in development context.

---

## 8. ASP.NET Monster & Production Deployment Plan

### 8.1 Hosting Constraints & Environment
- **Runtime:** .NET 10.0 ASP.NET Core Runtime.
- **SQLite Database Persistence:**
  - ASP.NET Monster and similar cloud app services typically provide ephemeral file systems unless persistent storage or a persistent app data mount is configured.
  - The SQLite database file (`OnlineFoodOrdering.db`) must reside in an app data directory with write permissions (`App_Data` or current working directory).
  - The connection string should remain flexible via `ConnectionStrings:DefaultConnection`.
- **Stripe Secret Configuration:**
  - `Stripe:SecretKey` and `Stripe:PublishableKey` must be read from environment variables in production (`STRIPE__SECRETKEY` / `STRIPE__PUBLISHABLEKEY`) rather than hardcoded in `appsettings.json`.
  - Fallback logic ensures local development doesn't crash when Stripe keys are empty (graceful warning on checkout).
- **Session & Cookies:**
  - `SameSite` and `Secure` cookie policies configured for HTTPS.
  - Idle session timeout set to 60 minutes.
- **Static Assets:**
  - Bundled CSS and JavaScript with Cache Busting (`asp-append-version="true"`).
  - No Node.js runtime or external npm build steps needed; all styles and scripts remain lightweight vanilla CSS and JavaScript compatible with native ASP.NET Core asset serving.

---

## 9. File-by-File Implementation Phases

### Phase 1: Database, Models & Backend Endpoints
- **Files Affected:**
  - `Models/CartItem.cs`: Add `ImagePath` property to support cart thumbnails.
  - `Models/Notification.cs`: Add `OrderId` and `Type` helper properties.
  - `Data/DbInitializer.cs` *(New)*: Idempotent seeder for default menu items and test accounts (`test@gmail.com`, `rifat@gmail.com`).
  - `Program.cs`: Wire up `DbInitializer`, configure cookie/session options, register JSON options for camelCase responses.
  - `Controllers/MainController.cs`:
    - Add `POST /Main/AddToCartAjax(int id)` returning `{ success, message, cartCount, cartTotal, item }`.
    - Add `POST /Main/UpdateCartItemQuantityAjax(int id, int delta)` returning updated line total and grand total.
    - Add `POST /Main/RemoveFromCartAjax(int id)` returning updated cart state.
    - Add `GET /Main/GetCartDrawerPartial` or JSON cart data endpoint.
    - Add `GET /Main/GetNotificationsAjax` returning unread count and latest items.
    - Add `POST /Main/MarkNotificationReadAjax(int id)`.
    - Add `POST /Main/MarkAllNotificationsReadAjax`.

### Phase 2: Design System, Layout & Global Components
- **Files Affected:**
  - `wwwroot/css/site.css`: Complete rewrite with CSS custom properties (Warm Ivory `#FBF9F5`, Chili Red `#D9381E`, Espresso `#1F1916`, Basil Green `#2E7D32`), typography, micro-interactions, responsive grid, drawer styles, badge animations, card hover effects.
  - `Views/Shared/_Layout.cshtml`:
    - Header redesign with brand mark, category quick links, search form.
    - Notification bell with dynamic badge and dropdown menu.
    - Cart button with live item-count badge.
    - Slide-in cart drawer (`#cartDrawer`) markup and overlay.
    - Toast container for alerts.
    - Responsive footer with `© 2026 Rifat. All rights reserved.`, GitHub repo link, and Portfolio link.
  - `wwwroot/js/site.js`: Comprehensive client script for cart animations, AJAX drawer management, quantity stepper, notification dropdown polling, and toast triggers.

### Phase 3: Customer Journey Views Redesign
- **Files Affected:**
  - `Views/Main/Index.cshtml`: Hero section, category filter pills, responsive food card grid with image loading optimization, price pills, and AJAX add-to-cart triggers.
  - `Views/Main/Cart.cshtml`: Two-column modern cart table, item quantity buttons, sticky price breakdown, empty state illustration.
  - `Views/Main/Checkout.cshtml`: Payment method selector with custom radio cards (COD vs Stripe), clean delivery form, order item overview, test card helper.
  - `Views/Main/MyOrders.cshtml`: Visual order status progress tracking (Processing -> Confirmed -> Delivered), itemized table with thumbnails and total costs.
  - `Views/Main/StripeSuccess.cshtml` & `StripeCancel.cshtml`: Polished order confirmation and payment cancellation views.

### Phase 4: Authentication & Admin Dashboard Redesign
- **Files Affected:**
  - `Views/Account/Login.cshtml`: Split-card design with appetizing food visual, floating inputs, and validation messages.
  - `Views/Account/Register.cshtml`: Clean registration card matching brand theme.
  - `Views/Admin/Index.cshtml`: Executive dashboard with key performance metric tiles (Pending Orders, Delivered Orders, Total Items), and quick action buttons.
  - `Views/Admin/Orders.cshtml`: Filterable orders table with status badges, sorting buttons, and quick status update controls.
  - `Views/Admin/FoodList.cshtml`: Modern food catalog table with thumbnails, price pills, edit/delete modals.
  - `Views/Admin/AddFood.cshtml` & `EditFood.cshtml`: Clean card forms with live image upload previews.

---

## 10. Quality Assurance & Acceptance Test Plan

1. **Cart & Drawer Verification:**
   - [ ] Add item from homepage: Button shows feedback, badge count increments, drawer slides in without full-page reload.
   - [ ] Adjust quantity in drawer: Subtotal updates accurately based on server prices; quantity cannot drop below 1.
   - [ ] Remove item: Card animates out; badge updates; if empty, empty cart state renders.
   - [ ] Full Cart page (`/Main/Cart`): Matches drawer totals and links smoothly to Checkout.
2. **Notification System Verification:**
   - [ ] Placing an order creates an unread notification for the user.
   - [ ] Admin updating order status creates an unread notification for that user.
   - [ ] Unread badge displays accurate count on bell icon.
   - [ ] Clicking a notification marks it read and navigates to `/Main/MyOrders`.
   - [ ] "Mark all as read" clears all unread indicators for the logged-in user.
3. **Authentication & Authorization:**
   - [ ] Login as `test@gmail.com` / `1234`: Verifies customer role, hides admin links, allows ordering and order tracking.
   - [ ] Login as `rifat@gmail.com` / `11223344`: Verifies admin role, displays Admin Dashboard link, grants access to `/Admin/*`.
   - [ ] Non-admin visiting `/Admin/Orders` is safely redirected to `/Main/Index`.
4. **Payment & Checkout:**
   - [ ] Cash on Delivery order creates an order with status `"Processing"` and clears session cart.
   - [ ] Stripe online payment generates a session and handles successful return to `/Main/StripeSuccess`.
5. **Footer & Compliance:**
   - [ ] Displays exact text `© 2026 Rifat. All rights reserved.`
   - [ ] GitHub link points to `https://github.com/Rifat-Bin-Alam/Online-Food-Ordering-WebApp` with `target="_blank"`.
   - [ ] Portfolio link points to `https://rifat-bin-alam.vercel.app/` with `target="_blank"`.
6. **Responsiveness & Cross-Device Layout:**
   - [ ] Tested on Mobile (375px), Tablet (768px), and Desktop (1200px+).
   - [ ] No unintended horizontal scrolling.
   - [ ] Touch targets on mobile are at least 44x44px.

---

## 11. Technical Risks & Mitigation Strategies

1. **SQLite Concurrency & Multi-user Writes:**
   - *Risk:* SQLite database locking under concurrent checkout or status updates.
   - *Mitigation:* Enable WAL (Write-Ahead Logging) mode on SQLite connection and use short-lived DbContext lifecycles.
2. **Stripe Test API Key Availability:**
   - *Risk:* If live Stripe keys are not configured in `appsettings.json`, card checkout will fail with an exception.
   - *Mitigation:* Provide defensive checks in `MainController.Checkout` that display a friendly warning if `Stripe:SecretKey` is missing or invalid, allowing testing to proceed via Cash on Delivery without crashing.
3. **Session Loss on App Restart:**
   - *Risk:* Default in-memory session is wiped if the server process recycles on hosting providers like ASP.NET Monster.
   - *Mitigation:* Ensure user credentials and critical order info are persisted in SQLite, keeping session usage strictly for transient cart storage.
