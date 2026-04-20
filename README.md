# CineTrack

CineTrack je aplikacija za pracenje filmova: pregled filmova, watchlist i recenzije.

## Pokretanje

U root folderu projekta (`D:\MovieTracker`) pokreni:

```powershell
docker compose up --build -d
```

Posle toga:
- API + Swagger: http://localhost:5265/swagger
- Mongo Express: http://localhost:8081

## Frontend

```powershell
cd frontend
npm install
npm run dev
```

Frontend je na: http://localhost:5173

## Prijava (seed admin)

- email: `admin@movietracker.local`
- lozinka: `Admin123!`

## TMDb import

Za import filmova treba TMDb key u `.env` fajlu (root):

```env
TMDB_API_KEY=ovde_tvoj_kljuc
```

Zatim ponovo:

```powershell
docker compose up --build -d
```

Import endpoint (admin):
- `POST /api/admin/import/tmdb`

## Napomena

Nemoj commit-ovati `.env` i privatne kljuceve.
