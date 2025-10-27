# URLBox

**URLBox** is a modern .NET 7 web application that helps teams securely store, organize, and share important URLs across projects and environments.  
It provides authentication, team-based permissions, and a clean responsive interface — making link management effortless for development teams and organizations.

---

## 🚀 Features

- **Smart URL Management** – Save and categorize URLs by project, environment, and description.  
- **Team & Project Permissions** – Fine-grained access control using ASP.NET Identity roles and claims.  
- **Secure Authentication** – Built-in login, registration, and role management.  
- **Reliable Data Storage** – Entity Framework Core with SQL Server for seamless persistence.  
- **Elegant Interface** – Bootstrap-powered UI with modals, copy/open actions, and environment grouping.

---

## ⚙️ Setup

1. **Clone the Repository**
   ```bash
   git clone https://github.com/FindFara/URLBox.git
   cd URLBox
   ```

2. **Configure Database**
   Edit your `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.;Database=UrlDatabase;Trusted_Connection=True;"
   }
   ```
   *(Or set it via environment variable for security.)*

3. **Apply Migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```
   Then open [https://localhost:5001](https://localhost:5001) in your browser.

---

## 💡 Usage

- **Teams & Projects** – Create teams and organize URLs per project.  
- **Add URLs** – Include a description, project tag, and environment (Dev, Stage, Prod, etc.).  
- **Manage Access** – Assign roles (Admin, Manager, Member) to control who can view or modify content.  

---

## 🛠 Tech Stack

- **Backend:** ASP.NET Core 7, Entity Framework Core, .NET Identity  
- **Database:** Microsoft SQL Server  
- **Frontend:** Bootstrap 5, jQuery, Razor Views  

---

## 🤝 Contributing

1. Fork the repository  
2. Create a feature branch  
3. Commit your changes  
4. Open a pull request  

Please follow the existing coding style and test before submission.

---

## 📄 License

All rights reserved © FindFara. 
