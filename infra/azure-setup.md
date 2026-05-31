# Azure Setup Guide

One-time manual setup to provision the infrastructure for MentoringApp.
Run these commands in Azure CLI (`az login` first).

## Variables — set once

```bash
LOCATION="eastus"
RG="mentoring-rg"
ACR="mentoringappacr"          # must be globally unique, lowercase, alphanumeric
ENV="mentoring-env"
API_APP="mentoring-api"
WEB_APP="mentoring-web"
```

## 1. Resource Group

```bash
az group create --name $RG --location $LOCATION
```

## 2. Azure Container Registry

```bash
az acr create --name $ACR --resource-group $RG --sku Basic --admin-enabled true
```

Save the credentials for GitHub Secrets:
```bash
az acr credential show --name $ACR --query "{username:username, password:passwords[0].value}"
# ACR_LOGIN_SERVER = <acr-name>.azurecr.io
# ACR_USERNAME     = output username
# ACR_PASSWORD     = output password
```

## 3. Container Apps Environment

```bash
az containerapp env create \
  --name $ENV \
  --resource-group $RG \
  --location $LOCATION
```

## 4. Deploy API Container App (initial)

```bash
az containerapp create \
  --name $API_APP \
  --resource-group $RG \
  --environment $ENV \
  --image mcr.microsoft.com/dotnet/samples:aspnetapp \
  --target-port 8080 \
  --ingress external \
  --registry-server ${ACR}.azurecr.io \
  --registry-username $(az acr credential show --name $ACR --query username -o tsv) \
  --registry-password $(az acr credential show --name $ACR --query passwords[0].value -o tsv) \
  --env-vars \
    "ConnectionStrings__DefaultConnection=Data Source=/app/data/mentoring.db" \
    "AppSettings__RecreateDbOnStartup=false" \
    "AppSettings__SkipVerificationCode=false" \
    "AppSettings__AdminEmail=school@gmail.com"
```

Note the API's public FQDN:
```bash
az containerapp show --name $API_APP --resource-group $RG --query properties.configuration.ingress.fqdn -o tsv
# → mentoring-api.xxxx.eastus.azurecontainerapps.io
```

## 5. Deploy Web Container App (initial)

```bash
az containerapp create \
  --name $WEB_APP \
  --resource-group $RG \
  --environment $ENV \
  --image mcr.microsoft.com/dotnet/samples:aspnetapp \
  --target-port 8080 \
  --ingress external \
  --registry-server ${ACR}.azurecr.io \
  --registry-username $(az acr credential show --name $ACR --query username -o tsv) \
  --registry-password $(az acr credential show --name $ACR --query passwords[0].value -o tsv) \
  --env-vars \
    "ApiSettings__BaseUrl=https://<API_FQDN_FROM_ABOVE>"
```

Note the Web's FQDN for CORS:
```bash
az containerapp show --name $WEB_APP --resource-group $RG --query properties.configuration.ingress.fqdn -o tsv
# → mentoring-web.xxxx.eastus.azurecontainerapps.io
```

## 6. Set JWT Secret (as a Container App secret)

```bash
az containerapp secret set \
  --name $API_APP \
  --resource-group $RG \
  --secrets "jwt-secret=<YOUR_STRONG_32_CHAR_SECRET>"

az containerapp update \
  --name $API_APP \
  --resource-group $RG \
  --set-env-vars "JwtSettings__Secret=secretref:jwt-secret"
```

## 7. Update CORS on API to allow Web origin

```bash
az containerapp update \
  --name $API_APP \
  --resource-group $RG \
  --set-env-vars "AllowedOrigins=https://<WEB_FQDN>"
```

## 8. GitHub Secrets

In your GitHub repo → Settings → Secrets and variables → Actions, add:

| Secret name        | Value                                      |
|--------------------|--------------------------------------------|
| `AZURE_CREDENTIALS`| JSON from step below                       |
| `ACR_LOGIN_SERVER` | `mentoringappacr.azurecr.io`               |
| `ACR_USERNAME`     | ACR admin username                         |
| `ACR_PASSWORD`     | ACR admin password                         |
| `JWT_SECRET`       | Strong 32+ char random string              |
| `EMAIL_FROM`       | SMTP sender address (e.g. `servicehandler055@gmail.com`) |
| `EMAIL_PASSWORD`   | Gmail App Password for the sender account  |
| `ADMIN_EMAIL`      | School admin email address                 |
| `ALLOWED_ORIGINS`  | `https://<WEB_FQDN>`                       |
| `API_BASE_URL`     | `https://<API_FQDN>`                       |

Generate `AZURE_CREDENTIALS`:
```bash
az ad sp create-for-rbac \
  --name "mentoring-github-actions" \
  --role contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/$RG \
  --sdk-auth
```
Copy the full JSON output as the `AZURE_CREDENTIALS` secret.

## 9. SQLite Persistence (optional, recommended for production)

By default the DB is bundled in the Docker image. To persist data across deployments:

```bash
# Create Azure Storage account and file share
STORAGE="mentoringstorage"
az storage account create --name $STORAGE --resource-group $RG --sku Standard_LRS
az storage share create --name mentoring-data --account-name $STORAGE

# Link storage to Container Apps environment
az containerapp env storage set \
  --name $ENV --resource-group $RG \
  --storage-name mentoring-data \
  --azure-file-account-name $STORAGE \
  --azure-file-account-key $(az storage account keys list --account-name $STORAGE --query [0].value -o tsv) \
  --azure-file-share-name mentoring-data \
  --access-mode ReadWrite

# Mount in the API container app (requires YAML update — see Azure docs)
```

After mounting, set:
```bash
az containerapp update --name $API_APP --resource-group $RG \
  --set-env-vars "ConnectionStrings__DefaultConnection=Data Source=/mnt/data/mentoring.db"
```
