# CineTracker

CineTrack je full-stack aplikacija za pracenje filmova (watchlist, recenzije, ocene) sa .NET API + MongoDB + React frontend-om.

## Stack
- Backend: ASP.NET Core 10 + MongoDB.Driver
- Frontend: React + Vite + TypeScript
- DB: MongoDB (Docker)
- API docs: Swagger

## 1) Pokretanje preko Docker-a

Iz root foldera (`D:\MovieTracker`):

```powershell
docker compose up --build -d
```

Servisi:
- API: [http://localhost:5265/swagger](http://localhost:5265/swagger)
- Mongo Express: [http://localhost:8081](http://localhost:8081)
- Mongo conn: `mongodb://admin:admin123@localhost:27017/?authSource=admin`

## 2) Frontend razvoj

```powershell
cd frontend
npm install
npm run dev
```

## 3) Auth i admin

- Register: `POST /api/users/register`
- Login: `POST /api/users/login`
- Admin create: `POST /api/users/admin` (Admin token)

Seed admin (dev):
- email: `admin@movietracker.local`
- password: `Admin123!`

## 4) Testovi

```powershell
dotnet test D:\MovieTracker\MovieTracker.sln
```
