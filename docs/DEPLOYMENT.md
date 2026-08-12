# Deployment guide — Google Cloud Run

This document walks through the one-time GCP setup required for the CI/CD
pipeline in `.github/workflows/ci-cd.yml` to build, push and deploy the
app to Cloud Run without ever storing a service-account key on GitHub.

Read it once. Follow every command in order. Everything after that is
`git push origin main` → app deploys.

---

## Prerequisites

- `gcloud` CLI installed and authenticated as an account with **Project
  Owner** on your target GCP project (or granular equivalent).
- GitHub CLI (`gh`) installed, or willingness to click through the GitHub
  settings UI for secrets.
- A Neon Postgres project with a working direct URL.
- SMTP credentials (Gmail App Password or similar) if you want real
  email delivery.

---

## 0. Environment variables you'll reuse

Set these once in your shell so the rest of the guide is copy-paste:

```bash
export PROJECT_ID="gestor-financiero-prod"     # replace with your GCP project id
export PROJECT_NUMBER="$(gcloud projects describe $PROJECT_ID --format='value(projectNumber)')"
export REGION="us-central1"
export SERVICE="gestor-financiero"
export REPO="gestor-financiero"                # Artifact Registry repo name
export GITHUB_OWNER="Alejandro-Montepeque"     # your GitHub username / org
export GITHUB_REPO="gestor-financiero"         # your GitHub repository name
```

---

## 1. Enable the APIs

```bash
gcloud config set project $PROJECT_ID

gcloud services enable \
    run.googleapis.com \
    artifactregistry.googleapis.com \
    secretmanager.googleapis.com \
    iamcredentials.googleapis.com \
    sts.googleapis.com
```

---

## 2. Create the Artifact Registry repo

Docker images live here.

```bash
gcloud artifacts repositories create $REPO \
    --repository-format=docker \
    --location=$REGION \
    --description="Gestor Financiero container images"
```

---

## 3. Store the secrets in Secret Manager

Never bake secrets into the image or the workflow. Cloud Run injects
them at boot as env vars via `--set-secrets` (already wired in the
deploy step).

### 3.1 Neon Postgres connection string

```bash
# Paste your Neon direct URL when prompted (no trailing newline).
# Format: postgresql://user:pass@host.neon.tech/db?sslmode=require
printf "%s" "postgresql://USER:PASS@HOST.neon.tech/gestor_financiero?sslmode=require" \
    | gcloud secrets create neon-connection-string --data-file=-
```

### 3.2 SMTP password (optional — only if `Smtp__UseFakeSender=false`)

```bash
printf "%s" "your-16-char-gmail-app-password" \
    | gcloud secrets create smtp-password --data-file=-
```

### Updating a secret later

```bash
printf "%s" "new-value" | gcloud secrets versions add neon-connection-string --data-file=-
```

Cloud Run picks up the new version on the next deploy (or if you pin
`:latest`, on the next container cold start).

---

## 4. Service account for Cloud Run

The Cloud Run runtime uses this identity to read Secret Manager values
and write logs.

```bash
gcloud iam service-accounts create $SERVICE-runtime \
    --display-name="Gestor Financiero — Cloud Run runtime"

RUNTIME_SA="$SERVICE-runtime@$PROJECT_ID.iam.gserviceaccount.com"

# Read the two secrets we created.
gcloud secrets add-iam-policy-binding neon-connection-string \
    --member="serviceAccount:$RUNTIME_SA" \
    --role="roles/secretmanager.secretAccessor"

gcloud secrets add-iam-policy-binding smtp-password \
    --member="serviceAccount:$RUNTIME_SA" \
    --role="roles/secretmanager.secretAccessor"
```

---

## 5. Workload Identity Federation for GitHub Actions

This is the modern, keyless auth flow. GitHub gets a short-lived token,
exchanges it for a GCP token, and impersonates a deployer service
account. No JSON key files anywhere.

### 5.1 Create the Workload Identity Pool

```bash
gcloud iam workload-identity-pools create github-actions \
    --location=global \
    --display-name="GitHub Actions"
```

### 5.2 Create the OIDC provider

```bash
gcloud iam workload-identity-pools providers create-oidc github \
    --location=global \
    --workload-identity-pool=github-actions \
    --display-name="GitHub OIDC" \
    --issuer-uri="https://token.actions.githubusercontent.com" \
    --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository,attribute.repository_owner=assertion.repository_owner,attribute.ref=assertion.ref" \
    --attribute-condition="assertion.repository_owner == '$GITHUB_OWNER'"
```

The `attribute-condition` restricts which GitHub repos can use this
provider. Only your account/org can mint tokens.

### 5.3 Create the deployer service account

```bash
gcloud iam service-accounts create $SERVICE-deployer \
    --display-name="Gestor Financiero — GitHub Actions deployer"

DEPLOYER_SA="$SERVICE-deployer@$PROJECT_ID.iam.gserviceaccount.com"
```

### 5.4 Give the deployer SA the roles it needs

```bash
# Push images to Artifact Registry.
gcloud artifacts repositories add-iam-policy-binding $REPO \
    --location=$REGION \
    --member="serviceAccount:$DEPLOYER_SA" \
    --role="roles/artifactregistry.writer"

# Deploy new Cloud Run revisions.
gcloud projects add-iam-policy-binding $PROJECT_ID \
    --member="serviceAccount:$DEPLOYER_SA" \
    --role="roles/run.admin"

# Cloud Run deploys "run as" the runtime SA, so the deployer needs
# permission to act as that identity.
gcloud iam service-accounts add-iam-policy-binding $RUNTIME_SA \
    --member="serviceAccount:$DEPLOYER_SA" \
    --role="roles/iam.serviceAccountUser"
```

### 5.5 Allow GitHub to impersonate the deployer

```bash
gcloud iam service-accounts add-iam-policy-binding $DEPLOYER_SA \
    --role="roles/iam.workloadIdentityUser" \
    --member="principalSet://iam.googleapis.com/projects/$PROJECT_NUMBER/locations/global/workloadIdentityPools/github-actions/attribute.repository/$GITHUB_OWNER/$GITHUB_REPO"
```

---

## 6. Push the GitHub Actions secrets

The workflow needs three values as GitHub Actions **variables** (not
secrets — they're project identifiers, not credentials):

```bash
gh variable set GCP_PROJECT_ID       --body "$PROJECT_ID"
gh variable set GCP_REGION           --body "$REGION"
gh variable set GCP_WIF_PROVIDER     --body "projects/$PROJECT_NUMBER/locations/global/workloadIdentityPools/github-actions/providers/github"
gh variable set GCP_DEPLOYER_SA      --body "$DEPLOYER_SA"
gh variable set GCP_RUNTIME_SA       --body "$RUNTIME_SA"
gh variable set GCP_ARTIFACT_REPO    --body "$REPO"
gh variable set CLOUD_RUN_SERVICE    --body "$SERVICE"
```

If you don't have `gh`, add them at:
`Settings → Secrets and variables → Actions → Variables (Repository)`.

---

## 7. First deploy (manual sanity check)

Before turning on CI/CD, do one manual deploy so the Cloud Run service
exists. This creates the `gestor-financiero` service under Cloud Run and
lets you set the initial config.

```bash
# Build + push locally (uses your Docker Desktop).
IMAGE="$REGION-docker.pkg.dev/$PROJECT_ID/$REPO/app:manual"
make docker-build IMAGE=$IMAGE

gcloud auth configure-docker "$REGION-docker.pkg.dev"
docker push $IMAGE

# Create the service.
gcloud run deploy $SERVICE \
    --image $IMAGE \
    --region $REGION \
    --port 8080 \
    --allow-unauthenticated \
    --service-account $RUNTIME_SA \
    --min-instances 0 \
    --max-instances 3 \
    --cpu 1 \
    --memory 512Mi \
    --set-env-vars "ASPNETCORE_ENVIRONMENT=Production" \
    --set-secrets "ConnectionStrings__Default=neon-connection-string:latest,Smtp__Password=smtp-password:latest"
```

At the end you'll get a `https://gestor-financiero-XXXXXXXX-uc.a.run.app`
URL — open it. If it loads: 🎉.

---

## 8. Cutover to CI/CD

From now on, `git push origin main` triggers the pipeline. It:

1. Restores + builds + runs tests.
2. If on `main`: builds the Docker image tagged with the commit SHA.
3. Pushes to Artifact Registry.
4. Deploys a new Cloud Run revision.
5. Reports the deployed URL in the Actions summary.

Pull requests only run the build/test step.

---

## Troubleshooting

**`Permission 'iam.serviceAccounts.getAccessToken' denied`** — the
`principalSet://` binding in step 5.5 is wrong. Double-check the repo
owner and repo name match exactly (case-sensitive on GitHub).

**Cloud Run revision starts but returns 502** — the container isn't
listening on `$PORT`. Program.cs already binds Kestrel to `$PORT` if
set, so most likely the DB secret is unreadable → app crashes on
startup. Check `Logs Explorer` in Cloud Run for the .NET exception.

**`unauthorized: authentication required` when pushing to Artifact
Registry** — you skipped `gcloud auth configure-docker`. Rerun it.
