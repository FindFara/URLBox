**URLBox**

URLBox is a modern .NET 7 web application designed to help teams securely store, organize, and share important URLs across projects and environments. With built-in authentication, team-based permissions, and a clean responsive UI, it simplifies link management for development teams and organizations.

🚀 Features

- **Smart URL Management** – Save and categorize URLs by project, environment, and description.
- **Team & Project Permissions** – Fine-grained access control using ASP.NET Identity roles and claims.
- **Secure Authentication** – User registration, login, and role management backed by Identity.  
- **Robust Persistence** – Powered by Entity Framework Core and SQL Server with migrations support.
- **Elegant Interface** – Bootstrap-based responsive UI with modals, copy/open actions, and environment grouping.

**⚙️ Setup**
1. **Clone the repository**
   ```bash
   git clone https://github.com/FindFara/URLBox.git
   cd URLBox
  
 
 2. **Configure your database**
 
Edit `appsettings.json` or set an environment variable:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=UrlDatabase;Trusted_Connection=True;"
  }
}


