# Quick CodeQL Security Analysis Commands

# Create database and run security analysis
codeql database create --language=csharp --source-root=. --command="dotnet build --no-restore" ./codeql-database

# Run comprehensive security and quality analysis
codeql database analyze ./codeql-database codeql/csharp-queries:codeql-suites/csharp-security-and-quality.qls --format=sarif-latest --output=security-results.sarif

# Run specific security queries
codeql database analyze ./codeql-database codeql/csharp-queries:Security/CWE/CWE-079/XSS.ql --format=csv --output=xss-results.csv
codeql database analyze ./codeql-database codeql/csharp-queries:Security/CWE/CWE-089/SqlInjection.ql --format=csv --output=sql-injection-results.csv

# View results in VS Code or upload to GitHub