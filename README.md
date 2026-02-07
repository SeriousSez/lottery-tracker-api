# Lottery Tracker API

A RESTful API built with ASP.NET Core 8.0 for tracking lottery drawings and providing statistical analysis on lottery numbers. The API scrapes lottery data, stores it in a MySQL database, and offers analytics endpoints for frequency analysis, hot/cold numbers, and more.

## Features

- 📊 **Lottery Drawing Management** - Store and retrieve lottery drawing results
- 🔍 **Web Scraping** - Automatically fetch lottery results from Danish lottery sources
- 📈 **Statistical Analysis** - Calculate number frequencies, hot/cold numbers, and patterns
- 🗄️ **MySQL Database** - Persistent storage using Entity Framework Core
- 📝 **Swagger Documentation** - Interactive API documentation
- 🌐 **CORS Support** - Ready for frontend integration
- 📋 **Structured Logging** - Using Serilog for comprehensive logging

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL Server 5.7+ or MariaDB 10.3+
- A code editor (Visual Studio, VS Code, Rider, etc.)

## 🔒 Security Note

**Never commit sensitive credentials to Git!** This project uses:
- User Secrets for development
- Environment variables for production
- `.gitignore` to protect configuration files

See [SECURITY-SETUP.md](SECURITY-SETUP.md) for detailed security configuration.

## Installation

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd LotteryTracker.API
   ```

2. **Configure the database connection**

   **Option A: Using User Secrets (Recommended for Development)**
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=lottery_db;Uid=your_user;Pwd=your_password;"
   ```

   **Option B: Using appsettings.Development.json (Not committed to Git)**
   
   Copy `appsettings.Example.json` to `appsettings.Development.json` and update with your credentials:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=lottery_db;Uid=your_user;Pwd=your_password;"
     }
   }
   ```

   **Option C: Using Environment Variables**
   ```powershell
   $env:ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=lottery_db;Uid=your_user;Pwd=your_password;"
   ```

3. **Apply database migrations**

   ```bash
   dotnet ef database update
   ```

4. **Run the application**

   ```bash
   dotnet run
   ```

   The API will be available at:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### Drawings Controller

#### Get All Drawings

```http
GET /api/drawings?limit=100
```

Retrieves lottery drawings ordered by date (most recent first).

**Query Parameters:**

- `limit` (optional, default: 100) - Number of drawings to retrieve

**Response:**

```json
[
  {
    "id": "guid",
    "drawDate": "2026-02-07T00:00:00Z",
    "winningNumbers": [3, 12, 18, 25, 31, 42, 7],
    "createdAt": "2026-02-07T10:30:00Z",
    "updatedAt": "2026-02-07T10:30:00Z"
  }
]
```

#### Get Drawing by ID

```http
GET /api/drawings/{id}
```

#### Scrape Latest Drawings

```http
POST /api/drawings/scrape
```

Fetches the latest lottery drawings from the source and stores them in the database.

**Query Parameters:**

- `count` (optional, default: 10) - Number of recent drawings to fetch

#### Get Latest Drawing

```http
GET /api/drawings/latest
```

Returns the most recent lottery drawing.

### Analytics Controller

#### Get Number Frequencies

```http
GET /api/analytics/frequencies
```

Returns frequency count for all lottery numbers.

**Response:**

```json
[
  {
    "number": 7,
    "count": 145,
    "percentage": 12.5
  }
]
```

#### Get Hot Numbers

```http
GET /api/analytics/frequencies/hot?count=10
```

Returns the most frequently drawn numbers.

**Query Parameters:**

- `count` (optional, default: 10) - Number of hot numbers to return

#### Get Cold Numbers

```http
GET /api/analytics/frequencies/cold?count=10
```

Returns the least frequently drawn numbers.

**Query Parameters:**

- `count` (optional, default: 10) - Number of cold numbers to return

#### Get Consecutive Number Pairs

```http
GET /api/analytics/consecutive-pairs
```

Analyzes and returns consecutive number pairs that appear together.

#### Get Number Pair Frequencies

```http
GET /api/analytics/pairs?minCount=2
```

Returns frequency of number pairs appearing together.

**Query Parameters:**

- `minCount` (optional, default: 2) - Minimum occurrence count

#### Get Analysis Summary

```http
GET /api/analytics/summary
```

Returns a comprehensive statistical summary including hot/cold numbers, most common pairs, and overview statistics.

## Database Schema

### LotteryDrawing Table

- `Id` (GUID) - Primary key
- `DrawDate` (DateTime) - Date of the lottery drawing
- `WinningNumbers` (JSON Array) - Array of winning numbers
- `CreatedAt` (DateTime) - Record creation timestamp
- `UpdatedAt` (DateTime) - Record update timestamp

## Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: MySQL with Pomelo.EntityFrameworkCore.MySql
- **ORM**: Entity Framework Core 8.0
- **Logging**: Serilog
- **API Documentation**: Swagger/OpenAPI
- **HTTP Client**: Built-in HttpClient with custom headers

## Development

### Running in Development Mode

```bash
dotnet run --environment Development
```

### Creating New Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Running Tests

```bash
dotnet test
```

## Project Structure

```
LotteryTracker.API/
├── Controllers/          # API Controllers
│   ├── DrawingsController.cs
│   └── AnalyticsController.cs
├── Data/                # Database Context
│   └── LotteryDbContext.cs
├── Models/              # Data Models
│   ├── LotteryDrawing.cs
│   └── StatisticalAnalysis.cs
├── Services/            # Business Logic
│   ├── DanishLotteryScraper.cs
│   └── StatisticalAnalysisService.cs
├── Migrations/          # EF Core Migrations
├── appsettings.json     # Configuration
└── Program.cs           # Application Entry Point
```

## Configuration

### Logging Levels

Configure logging levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### CORS Policy

The API includes CORS support for frontend integration. Configure allowed origins in `Program.cs`.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or contributions, please open an issue in the repository.

## Roadmap

- [ ] Add support for multiple lottery types
- [ ] Implement prediction algorithms
- [ ] Add user authentication and favorites
- [ ] Create historical trend analysis
- [ ] Add email notifications for specific number combinations
- [ ] Implement caching for improved performance
