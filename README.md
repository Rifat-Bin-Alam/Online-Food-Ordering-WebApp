# 🍽 Gourmet Bites — Online Food Ordering WebApp

**Live Link: http://onlinefoodorderingbyrifat.runasp.net/**

A full-stack, artisanal food-ordering e-commerce web application architected with **ASP.NET Core MVC (.NET 10.0)**, **Entity Framework Core**, **SQLite**, and **Stripe Checkout**. Designed with a warm culinary aesthetic, responsive slide-in cart drawer, live notification system, comprehensive customer checkout flow, and an administrator management portal.

---

## 🌟 Key Features

### 👤 Customer Experience
- **Artisanal Food Catalog:** Appetizing dish presentation with food photography, descriptions, pricing, and live search.
- **Client-Side Category Filters:** Instant filtering by food categories without full-page reloads.
- **Interactive Slide-in Cart Drawer:** 
  - Dynamic drawer accessible from any page.
  - Animated item count badge with pop feedback.
  - Asynchronous quantity steppers (`+` / `−`) and remove actions without refreshing.
  - Free delivery milestone indicator.
- **Full Shopping Cart & Checkout:**
  - Dedicated cart view with order breakdown.
  - Multi-method checkout supporting **Stripe Online Payment** and **Cash on Delivery (COD)**.
- **Order History & Tracking:** Detailed timeline and status tracking for customer orders (`Pending`, `Processing`, `Delivered`, `Cancelled`).
- **Real-Time Notification System:**
  - Persistent bell dropdown in the top navigation bar.
  - Unread badge counter with wiggle animation on new alerts.
  - Interactive *"Mark as read"* and *"Mark all read"* actions backed by AJAX endpoints.
  - Background polling every 45 seconds for active users.
- **Demo Mode Login Helpers:** Convenient copy and one-click form-fill buttons for demo customer and administrator accounts.
- **Sticky "Back to Top" Floating Control:** Smooth scroll restoration, 300px trigger threshold, safe-area inset support, and `prefers-reduced-motion` compliance.

### 🛡️ Administrator Management
- **Analytics Dashboard:** Order metrics, revenue breakdown, and order status summaries.
- **Order Processing:** Update order fulfillment states and inspect customer shipping details.
- **Food Catalog Management:** View culinary items, upload dish photography, and configure menu pricing.
- **Demo Mode Safeguards:** Built-in configurable demo protection (`"DemoMode": true`) preventing unauthorized database alterations during public demonstrations.

---

## 🔑 Demo Login Credentials

The application includes an automated database initializer (`DbInitializer.cs`) that provisions demonstration credentials on initial startup:

| Role | Email | Password | Access Level |
| :--- | :--- | :--- | :--- |
| **Customer** | `test@gmail.com` | `1234` | Menu browsing, cart drawer, checkout, order tracking, notifications |
| **Administrator** | `rifat@gmail.com` | `11223344` | Full access: dashboard, order management, food catalog |

> [!NOTE]
> The login page includes interactive quick-fill and copy buttons to easily test either account without typing credentials.

---

## 🛠️ Technology Stack

- **Backend Framework:** ASP.NET Core MVC (.NET 10.0)
- **Data Access & ORM:** Entity Framework Core 10 with SQLite provider
- **Payment Processing:** Stripe.net API (Stripe Checkout Sessions)
- **Frontend Engine:** Razor Views (.cshtml), HTML5, CSS3 Custom Properties, Vanilla JavaScript (ES6+)
- **Styling & Components:** Bootstrap 5, custom Gourmet Bites design system
- **Session Management:** Secure HTTP-only cookies with essential session state

---

## 📋 Prerequisites

Before running the application, ensure the following are installed on your machine:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or latest compatible .NET SDK)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Version 17.10+ with the **ASP.NET and web development** workload) or **Visual Studio Code** / **JetBrains Rider**
- [Git](https://git-scm.com/)

---

## 🚀 How to Run the Project

### Method A: Running via Visual Studio

1. **Open the Solution:**
   - Double-click `OnlineFoodOrdering.slnx` or open Visual Studio and select **File > Open > Project/Solution**, then select `OnlineFoodOrdering.slnx`.
2. **Restore Dependencies & Build:**
   - Visual Studio automatically restores NuGet packages on solution load.
   - Press <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>B</kbd> to build the solution.
3. **Run the Project:**
   - Select the **https** or **http** launch profile from the toolbar.
   - Press <kbd>F5</kbd> (Debug) or <kbd>Ctrl</kbd> + <kbd>F5</kbd> (Run without debugging).
   - The browser opens automatically at `https://localhost:7111` or `http://localhost:5173`.
   - The SQLite database (`OnlineFoodOrdering.db`) and seed data are automatically generated on first startup!

---

### Method B: Running via .NET CLI / Terminal

1. **Open your terminal** (PowerShell, Command Prompt, or bash) in the project root directory:
   ```bash
   cd "F:\IUBAT\9th Semester\CSC 440\Project\OnlineFoodOrdering"
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

3. **Build the application:**
   ```bash
   dotnet build -c Release
   ```

4. **Run the application:**
   ```bash
   dotnet run -c Release
   ```
   Or specify explicit local URLs:
   ```bash
   dotnet run -c Release --urls "http://127.0.0.1:5173;https://localhost:7111"
   ```

5. **Access the application:**
   - Open your browser and navigate to:
     - **HTTP:** `http://127.0.0.1:5173`
     - **HTTPS:** `https://localhost:7111`

---

## 💳 Optional: Configuring Stripe Payments

By default, the application uses **Cash on Delivery (COD)** without requiring an external API key. To test live or test-mode **Stripe Checkout**:

1. Create a free account at [stripe.com](https://stripe.com).
2. Obtain your test API keys (**Publishable Key** and **Secret Key**) from the Stripe Dashboard.
3. Configure them safely using .NET Secret Manager (recommended for development):
   ```bash
   dotnet user-secrets set "Stripe:PublishableKey" "pk_test_your_key_here"
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_key_here"
   ```
   Alternatively, add them directly to `appsettings.json`:
   ```json
   "Stripe": {
     "PublishableKey": "pk_test_...",
     "SecretKey": "sk_test_..."
   }
   ```

> [!CAUTION]
> **API Security Best Practice:** Never commit live or real test Stripe Secret Keys (`sk_live_...` or `sk_test_...`) to a Git repository. Always use .NET User Secrets in local development and secure environment variables in hosting environments. If an API secret was ever accidentally pushed to a GitHub repository, immediately revoke and roll the key in your [Stripe Dashboard](https://dashboard.stripe.com/apikeys).

---

## 📁 Project Architecture & Directory Structure

```plaintext
OnlineFoodOrdering/
├── Controllers/
│   ├── AccountController.cs       # Authentication (Login, Register, Logout)
│   ├── AdminController.cs         # Dashboard, order status, food catalog & DemoMode guard
│   └── MainController.cs          # Catalog, cart drawer, checkout, Stripe & notifications
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext for Users, FoodItems, Orders
│   └── DbInitializer.cs           # Database creation & demo account seeder
├── Models/
│   ├── User.cs                    # Customer & Administrator user entity
│   ├── FoodItem.cs                # Culinary dish entity
│   ├── Order.cs                   # Order header entity
│   ├── OrderItem.cs               # Order line-item entity
│   ├── CartItem.cs                # Session cart model
│   └── Notification.cs            # Customer notification entity
├── Views/
│   ├── Account/                   # Login & Registration views with demo credential helpers
│   ├── Admin/                     # Dashboard, Manage Orders, Food Catalog & Add Food
│   ├── Main/                      # Homepage, Menu, Cart, Checkout, Order Tracking
│   └── Shared/
│       ├── _Layout.cshtml         # Master layout (Navbar, Cart Drawer, Back to Top, Footer)
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/
│   ├── css/
│   │   └── site.css               # Gourmet Bites design system & responsive styling
│   ├── js/
│   │   └── site.js                # Interactive cart drawer, notification polling & scroll engine
│   └── images/                    # Food photography assets
├── appsettings.json               # Database connection string, DemoMode, Stripe configuration
├── IMPLEMENTATION_PLAN.md         # Detailed development plan & architectural specifications
├── OnlineFoodOrdering.csproj      # .NET 10.0 project configuration & package references
├── OnlineFoodOrdering.slnx        # Solution file
└── README.md                      # Project documentation
```

---

## 👨‍💻 Author & Links

- **Developer:** Rifat Bin Alam
- **GitHub Repository:** [Online Food Ordering WebApp](https://github.com/Rifat-Bin-Alam/Online-Food-Ordering-WebApp)
- **Portfolio:** [rifat-bin-alam.vercel.app](https://rifat-bin-alam.vercel.app/)

© 2026 Rifat. All rights reserved.
