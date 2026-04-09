namespace PortfolioDemo.Services.Resume
{
    public class ResumeContactInfo
    {
        public string Name { get; init; } = "Monica Leigh A. Rivera";
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string Location { get; init; } = "Reston, VA";
    }

    public class ResumeExperienceItem
    {
        public string Title { get; init; } = string.Empty;
        public string Company { get; init; } = string.Empty;
        public string Period { get; init; } = string.Empty;
        public List<string> Bullets { get; init; } = new();
    }

    public class ResumeData
    {
        public ResumeContactInfo Contact { get; init; } = new();
        public string Summary { get; init; } = string.Empty;
        public List<ResumeExperienceItem> Experience { get; init; } = new();
        public string Education { get; init; } = string.Empty;
        public Dictionary<string, List<string>> Skills { get; init; } = new();

        public static ResumeData Build(string? email, string? phone)
        {
            return new ResumeData
            {
                Contact = new ResumeContactInfo
                {
                    Name = "Monica Leigh A. Rivera",
                    Email = email,
                    Phone = phone,
                    Location = "Reston, VA"
                },
                Summary = "Software Engineer II at Microsoft with deep expertise in cloud-based and distributed " +
                          "systems on Azure. Designs and ships production-quality C# and .NET solutions end-to-end " +
                          "— from requirements and architecture through deployment and operational health. Brings a " +
                          "\"learn-it-all\" mindset, a sharp eye for security and reliability, and a genuine passion " +
                          "for mentoring teammates and raising the engineering bar across the team. Approaches AI " +
                          "tooling with intentionality — using it to move faster on familiar problems and as a " +
                          "learning accelerator for new domains, while retaining full technical ownership of outcomes.",
                Experience = new List<ResumeExperienceItem>
                {
                    new ResumeExperienceItem
                    {
                        Title = "Software Engineer II",
                        Company = "Microsoft — Reston, VA",
                        Period = "Dec 2022 – Present",
                        Bullets = new List<string>
                        {
                            "Designed AI-driven workflows with Copilot Agents for inventory management",
                            "Integrates AI tooling to accelerate development workflows while maintaining full comprehension and ownership of generated outputs",
                            "Developed and maintained secure, high-performance C# web applications in Azure",
                            "Authored technical documentation and operational health reports",
                            "Implemented CI/CD pipelines and infrastructure-as-code for streamlined deployments",
                            "Engineered secure environments using network isolation and firewall/NSG configurations",
                            "Successfully started and led several efforts to boost Operational and Engineering Excellence metrics",
                            "Delivered expectations on Organization-wide Secure development practices",
                            "E2E development of Azure Web Applications from design to Production"
                        }
                    },
                    new ResumeExperienceItem
                    {
                        Title = "Site Reliability Engineer II",
                        Company = "Microsoft",
                        Period = "Jan 2021 – Dec 2022",
                        Bullets = new List<string>
                        {
                            "Architected and integrated microservices for scalable system design",
                            "Led troubleshooting and maintenance for cloud-based financial applications",
                            "Planned and executed cloud deployments for data center acquisition",
                            "Mentored service members in technical and professional growth",
                            "Authored SOPs and troubleshooting guides for secure cloud deployments",
                            "Refactored and debugged cloud applications using PowerShell, JavaScript, JSON, and C#"
                        }
                    },
                    new ResumeExperienceItem
                    {
                        Title = "Junior Software Engineer",
                        Company = "Applied Research Associates",
                        Period = "Jan 2019 – Dec 2020",
                        Bullets = new List<string>
                        {
                            "Upgraded legacy VB.NET plotting capabilities with modern web technologies",
                            "Led design and development of multiple software applications across diverse technology stacks",
                            "Authored documentation for over 100 proprietary services, SOPs, and troubleshooting guides",
                            "Successfully migrated and modernized legacy applications from 32-bit to 64-bit"
                        }
                    }
                },
                Education = "B.S. Computer Science — George Mason University, Fairfax, VA",
                Skills = new Dictionary<string, List<string>>
                {
                    ["Programming Languages"] = new List<string>
                    {
                        "C#", "C++", "Python", "Java", "JavaScript", "TypeScript",
                        "VB.NET", "SQL", "HTML/CSS", "XML", "JSON", "PowerShell"
                    },
                    ["Frameworks & Tools"] = new List<string>
                    {
                        ".NET / .NET Core", "ASP.NET Core", "REST APIs / Web APIs",
                        "Entity Framework Core", "Copilot / AI Agents", "Appium",
                        "WinAppDriver", "JUnit", "NUnit", "Qt Creator", "PySide2/PyQt5",
                        "Unity", "Android Studio", "WinForms", "Plotly.js", "Kendo", "CEFSharp"
                    },
                    ["Cloud & DevOps"] = new List<string>
                    {
                        "Azure Expert", "Azure DevOps", "Infrastructure-as-Code (Bicep / ARM)",
                        "Microservices Architecture", "Azure App Service", "Azure Functions",
                        "Azure Key Vault", "Network Security / NSG / Firewall",
                        "Application Insights / Monitoring", "Git / Version Control",
                        "Full Stack Web Development"
                    },
                    ["Testing & Automation"] = new List<string>
                    {
                        "Automated Regression Testing", "GUI Testing", "Unit Testing",
                        "Integration Testing", "CI/CD Pipeline Development", "XUnit", "Code Reviews"
                    },
                    ["Engineering Practices"] = new List<string>
                    {
                        "Agile / Scrum", "Design Patterns", "SOLID Principles", "System Design",
                        "Security Development Lifecycle (SDL)", "Secure Code Practices",
                        "Feature Ownership", "Technical Documentation", "Mentorship",
                        "Incident Response / On-Call", "Operational Excellence",
                        "Spec Driven Development"
                    },
                    ["AI & Modern Practices"] = new List<string>
                    {
                        "Agentic AI Tools", "Copilot Agents", "Visual Studio Development",
                        "Ethical AI Use", "AI Force Multiplier", "Technical AI Ownership"
                    }
                }
            };
        }
    }
}
