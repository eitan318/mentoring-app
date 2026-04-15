# GitLab CI/CD Variables

Set these in **Settings → CI/CD → Variables** in your GitLab project. Mark sensitive values as **Masked** and **Protected** (protected variables are only available on protected branches such as `main`).

| Variable | Description |
|---|---|
| `PROD_POSTGRES_CONNECTION` | Full Postgres connection string for production |
| `JWT_SECRET` | 32+ character random secret for JWT signing |
| `SMTP_HOST` | SMTP server hostname |
| `FROM_EMAIL` | Sender email address |
| `FROM_PASSWORD` | SMTP app password |
