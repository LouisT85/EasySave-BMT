# CentralLogService

Service HTTP minimal pour centraliser les logs EasySave.

- Endpoint de reception: `POST /logs`
- Healthcheck: `GET /health`
- Stockage: un fichier JSON par jour (`yyyy-MM-dd.json`) dans le dossier `LOG_STORAGE_PATH`.

## Build et run en local

```bash
dotnet run --project CentralLogService.csproj
```

Par defaut, le service ecoute sur `http://localhost:5000` (ou selon votre environnement ASP.NET) et ecrit dans `/app/logs` ou la variable `LOG_STORAGE_PATH`.

## Docker

Depuis le dossier `CentralLogService/` :

```bash
docker build -t easysave-central-log .
docker run -d --name central-log --rm -p 8080:8080 -v central_logs:/app/logs easysave-central-log
docker logs -f central-log
docker stop central-log
```

Endpoint cible EasySave:

`http://localhost:8080/logs`
