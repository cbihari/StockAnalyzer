# Production Deployment

## 1. Neon PostgreSQL

1. Create a Neon project and database.
2. Copy the pooled PostgreSQL connection URL.
3. Keep `sslmode=require` in the URL. The API converts `DATABASE_URL` to an
   Npgsql connection using `SSL Mode=VerifyFull`.
4. The API applies EF Core migrations during startup.

## 2. Render Services

1. In Render, create a Blueprint from this repository's `render.yaml`.
2. Provide the prompted secrets:
   - `DATABASE_URL`: Neon pooled connection URL.
   - `ALLOWED_ORIGINS`: Vercel production URL, for example
     `https://stock-analyzer.vercel.app`. Separate multiple origins with commas.
   - `OPENAI_API_KEY`: optional OpenAI API key for grounded AI responses.
3. Deploy `stockanalyzer-ml`, then `stockanalyzer-api`.
4. Confirm:
   - `https://stockanalyzer-ml.onrender.com/health`
   - `https://stockanalyzer-api.onrender.com/health`

The ML service stores trained models on the Render disk mounted at
`/var/data/models`. Render supplies `PORT`; both containers bind to it.

## 3. Vercel Frontend

1. Import the repository into Vercel.
2. Set the project Root Directory to `frontend`.
3. Vercel uses `frontend/vercel.json`, runs `npm run build`, and publishes
   `dist/frontend/browser`.
4. The production Angular build uses same-origin `/api` requests. Vercel proxies
   them to `https://stockanalyzer-api.onrender.com`.
5. If the Render API service name or domain changes, update the API destination
   in `frontend/vercel.json`.
6. Add the final Vercel URL to Render's `ALLOWED_ORIGINS` and redeploy the API.

## Local Development

Local Angular builds continue to use `http://localhost:8080`. Docker Compose
continues to run the frontend, API, ML service, and local PostgreSQL database.

```bash
docker compose up --build
```

## Production Variables

### .NET API

| Variable | Required | Description |
| --- | --- | --- |
| `DATABASE_URL` | Yes | Neon PostgreSQL pooled URL |
| `ML_SERVICE_URL` | Yes | Render private ML service host and port or URL |
| `ALLOWED_ORIGINS` | Yes | Comma-separated HTTPS frontend origins |
| `ASPNETCORE_ENVIRONMENT` | Yes | Set to `Production` |
| `PORT` | Render | HTTP port supplied by Render |

### FastAPI ML Service

| Variable | Required | Description |
| --- | --- | --- |
| `PYTHON_ENV` | Yes | Set to `production` |
| `MODEL_DIR` | Yes | Persistent model directory |
| `PORT` | Render | HTTP port supplied by Render |
| `OPENAI_API_KEY` | No | Enables OpenAI responses |
