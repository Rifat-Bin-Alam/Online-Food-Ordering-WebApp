# 🍔 Online Food Ordering System
A web-based Online Food Ordering System developed using **ASP.NET Core MVC**, **Entity Framework Core**, **SQLite**, and **Stripe Payment Integration**.

The application allows customers to browse food items, manage their shopping cart, place orders, and make online payments. It also provides an admin panel for managing food items and customer orders

---

## 🔐 Demo Login Credentials

The application includes separate access levels for **User** and **Admin**. Both use the same login page.

### 👤 User Account

| Field | Credentials |
|---|---|
| Email | `test@gmail.com` |
| Password | `1234` |

### 🛠️ Admin Account

| Field | Credentials |
|---|---|
| Email | `rifat@gmail.com` |
| Password | `11223344` |

> **Note:** These credentials are provided for demonstration and testing purposes only.
## 📌 Features

### 👤 Customer Features

- User registration and login
- Browse available food items
- View food information
- Add food items to cart
- Manage shopping cart
- Place food orders
- Online payment using Stripe
- View order information
- Session-based user/cart management

### 🛠️ Admin Features

- Admin dashboard
- Add new food items
- Edit existing food items
- View food list
- View customer orders
- Manage food-related information

### 💳 Payment

The application integrates **Stripe** to provide online payment functionality.

Stripe configuration is stored in `appsettings.json` and the secret key is loaded through the application's configuration system.

> ⚠️ Never commit your Stripe secret key to a public GitHub repository.

---

## 🧰 Technologies Used

| Technology | Purpose |
|---|---|
| C# | Programming Language |
| ASP.NET Core MVC | Web Application Framework |
| Entity Framework Core | ORM / Database Access |
| SQLite | Database |
| Razor Views | Frontend/UI |
| HTML & CSS | User Interface |
| JavaScript | Client-side functionality |
| Stripe | Online Payment |
| Sessions | User/Cart State Management |

---

## 📂 Project Structure

```text
OnlineFoodOrdering/
│
├── Controllers/
│   ├── AccountController.cs
│   ├── AdminController.cs
│   └── MainController.cs
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Migrations/
│
├── Models/
│   ├── CartItem.cs
│   ├── ErrorViewModel.cs
│   ├── FoodItem.cs
│   ├── Notification.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   └── User.cs
│
├── Views/
│   ├── Account/
│   ├── Admin/
│   ├── Main/
│   └── Shared/
│
├── wwwroot/
│
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
├── OnlineFoodOrdering.csproj
└── OnlineFoodOrdering.db
