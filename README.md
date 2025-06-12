# PROJEKT LAB BACKEND
Marcin Świderski 15283

Paweł Rodzaj 15245
## SPOTIFY HISTORY API

Prosty Projekt CRUD wykorzystujący ASP.NET Web API i bazę danych PostgreSQL.
Projekt został wykonany w technologii .NET 8.0

## Uruchamianie
1. Sama Baza danych PostgreSQL jest opisana w plikach `docker-compose.db.yaml` oraz `docker-compose.prod.yaml`.
Aby odpalić samą Bazę, uzywamy `docker compose -f docker-compose.db.yaml up -d`, i by odpalić bazę + projekt, analogicznie
`docker compose -f docker-compose.prod.yaml up -d`
2. Po pierwszym uruchomieniu, przez pierwsze kilka chwil odbędzie się seedowanie bazy danych, więc trzeba chwilę poczekać.
 (Nie jest konieczny manualny import czy wrzucanie pliku csv, jest on już w plikach projektu)
3. Aby sprawdzić działanie API, można użyć Postmana (port 5000) lub wejść na swaggera (port 7001) (porty są już sforwardowane).

## Autoryzacja
Aby móc korzystać z niektórych endpointów API, należy się zalogować. W tym celu należy wysłać POST na `/api/auth/login` z danymi użytkownika.
(Domyślnie Admin:Admin123!)

