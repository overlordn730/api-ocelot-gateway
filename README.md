# Api.Gateway

API Gateway desarrollado en .NET 10 con Ocelot para enrutamiento, autenticación JWT y seguridad.

## Stack tecnológico

- .NET 10
- Ocelot (API Gateway)
- JWT Bearer Authentication
- Redis (lista negra de tokens)
- Serilog

## Requisitos previos

- .NET 10 SDK
- Redis (Docker recomendado)
- Api.Auth corriendo en puerto 7173
- Api.Exchange corriendo en puerto 7294

## Cómo correr el proyecto

1. Clonar el repositorio
2. Levantar Redis:
```bash
   docker run -d -p 6379:6379 --name redis redis:latest
```
3. Completar las credenciales en `src/Gateway/appsettings.Development.json`:
```json
   {
     "ConnectionStrings": {
       "Redis": "localhost:6379"
     },
     "JwtConfiguration": {
       "Secret": "TU_SECRET_MINIMO_32_CARACTERES",
       "Issuer": "Api.Auth",
       "Audience": "Api.Exchange"
     },
     "ApiExchangeKey": "TU_API_KEY"
   }
```
4. Ejecutar el gateway:
```bash
   dotnet run --project src/Gateway/Gateway.csproj
```
5. El gateway estará disponible en:
https://localhost:5000

## Enrutamiento

| Upstream | Downstream | Autenticación |
|---|---|---|
| POST /auth/login | Api.Auth /v1/api/auth/login | No |
| POST /auth/logout | Api.Auth /v1/api/auth/logout | Bearer JWT |
| POST /auth/refresh | Api.Auth /v1/api/auth/refresh | No |
| GET /users | Api.Exchange /v1/api/users | Bearer JWT |
| POST /users | Api.Exchange /v1/api/users | Bearer JWT |
| GET /users/{id} | Api.Exchange /v1/api/users/{id} | Bearer JWT |
| PUT /users/{id} | Api.Exchange /v1/api/users/{id} | Bearer JWT |
| DELETE /users/{id} | Api.Exchange /v1/api/users/{id} | Bearer JWT |
| GET /users/{userId}/addresses | Api.Exchange | Bearer JWT |
| POST /users/{userId}/addresses | Api.Exchange | Bearer JWT |
| PUT /addresses/{id} | Api.Exchange | Bearer JWT |
| DELETE /addresses/{id} | Api.Exchange | Bearer JWT |
| GET /currencies | Api.Exchange | Bearer JWT |
| POST /currencies | Api.Exchange | Bearer JWT |
| POST /currency/convert | Api.Exchange | Bearer JWT |

## Qué está implementado

- ✅ Enrutamiento con Ocelot
- ✅ Validación JWT
- ✅ Lista negra de tokens con Redis
- ✅ Inyección automática de API Key hacia Api.Exchange
- ✅ CORS