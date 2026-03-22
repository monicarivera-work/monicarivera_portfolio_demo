# Monica Rivera — Portfolio Demo

> A Razor Pages / ASP.NET Core 8 portfolio application deployed to Azure Web App Service, demonstrating software design abilities for potential employers and interviewers.

[![Build and Deploy](https://github.com/monicarivera-work/monicarivera_portfolio_demo/actions/workflows/main_monicarivera-portfolio-demo.yml/badge.svg)](https://github.com/monicarivera-work/monicarivera_portfolio_demo/actions/workflows/main_monicarivera-portfolio-demo.yml)

## 🚀 Live Demo

<!-- Add your Azure Web App URL here once deployed -->
> **Coming soon** — will be hosted on Azure Web App Service

## 📋 About

This portfolio showcases Monica Rivera's software engineering background, experience, and skills. Built as a Razor Pages web application to demonstrate proficiency in:

- **ASP.NET Core 8 / Razor Pages** — server-side rendered web application
- **Azure Web App Service** — cloud hosting with CI/CD via GitHub Actions
- **Azure File Storage** — serving downloadable resume files
- **Responsive CSS** — mobile-first, accessible design
- **GitHub Actions CI/CD** — automated build, test, and deploy pipeline

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 / Razor Pages |
| Language | C# 12 |
| Frontend | HTML5, CSS3, Bootstrap 5, JavaScript |
| Cloud | Azure Web App Service |
| Storage | Azure Files (resume downloads) |
| CI/CD | GitHub Actions |

## 📁 Project Structure

```
monicarivera_portfolio_demo/
├── .github/
│   ├── workflows/
│   │   └── main_monicarivera-portfolio-demo.yml  # CI/CD pipeline
│   ├── ISSUE_TEMPLATE/
│   └── PULL_REQUEST_TEMPLATE.md
├── PortfolioDemo/
│   ├── Pages/
│   │   ├── Shared/         # Layout, nav, footer
│   │   ├── Index.cshtml    # Home / hero
│   │   ├── About.cshtml    # Professional summary
│   │   ├── Experience.cshtml
│   │   ├── Skills.cshtml
│   │   ├── Education.cshtml
│   │   └── Contact.cshtml
│   ├── Controllers/        # File download controller
│   ├── wwwroot/            # Static assets (CSS, JS)
│   ├── Program.cs
│   └── PortfolioDemo.csproj
└── README.md
```

## 🏃 Running Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)

### Steps

```bash
# Clone the repo
git clone https://github.com/monicarivera-work/monicarivera_portfolio_demo.git
cd monicarivera_portfolio_demo

# Restore and run
cd PortfolioDemo
dotnet restore
dotnet run
```

Then open your browser to `https://localhost:5001` (or the port shown in the terminal).

## 🚢 Deployment

This app is deployed automatically to **Azure Web App Service** on every push to `main` via GitHub Actions.

**Required GitHub Secret:**
- `AZURE_WEBAPP_PUBLISH_PROFILE` — download from the Azure Portal → your Web App → "Get publish profile"

## 🤝 Contributing

See [PULL_REQUEST_TEMPLATE.md](.github/PULL_REQUEST_TEMPLATE.md) for contribution guidelines.

## 📄 License

[Apache 2.0](LICENSE)
