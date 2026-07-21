# Deploying CareLink to Azure (AKS + Azure SQL + Application Insights)

Two scripts take a clean Azure subscription to a running CareLink instance:

| Script | What it does | Run |
|--------|--------------|-----|
| `00-provision.sh` | Creates the resource group, ACR, AKS, Azure SQL, Log Analytics + Application Insights | once |
| `01-deploy.sh` | Builds & pushes the 3 images in ACR, then deploys to AKS (Azure SQL as the database) | every release |

The scripts never write secrets to disk. Only non-secret outputs (registry
host, App Insights connection string, SQL FQDN) land in `deploy/azure/.deploy.env`,
which is gitignored.

---

## Deploying on a free trial (interview demo)

The defaults are tuned for a **fresh Azure free account** ($200 credit, 30 days):
**1 AKS node** (Standard_B2s, 2 vCPU) and **1 replica** per service, which fits
the typical **4 vCPU regional quota** with headroom — no quota-increase request
needed. Azure SQL Basic + ACR Basic + 1 small node run comfortably inside the
credit, so the demo is **effectively free for the interview window**.

Check your quota before starting (look for `Total Regional vCPUs`, want ≥ 2 free):

```bash
az vm list-usage --location eastus -o table | grep -i "Total Regional vCPUs"
```

If it's 0, pick another region via `LOCATION=...`, or request an increase in the
portal (Subscription → Usage + quotas). **Remember to `az group delete` when the
interview is over** so the credit isn't consumed after the trial converts.

## Prerequisites

- An Azure subscription you can create resources in (a free trial is fine).
- **Azure Cloud Shell is the easiest option** — it already has `az`, `kubectl`,
  `helm`, and `kustomize`. Otherwise install all four locally.
  - `kustomize` here means the **standalone** binary (the scripts use
    `kustomize edit`, which `kubectl kustomize` does not provide).
- `openssl` (for generating the JWT secret).

```bash
az login
az account set --subscription "<your-subscription-id>"
```

---

## Step 1 — Provision infrastructure

Choose a strong SQL admin password (min 8 chars, upper + lower + digit + symbol),
then run:

```bash
cd deploy/azure
export SQL_ADMIN_PASSWORD='<strong-sql-admin-password>'
./00-provision.sh
```

Optional overrides (defaults in parentheses): `LOCATION` (eastus),
`RESOURCE_GROUP` (carelink-rg), `AKS_NODE_COUNT` (1 — free-trial-friendly;
use 2+ for production HA), `AKS_NODE_SIZE` (Standard_B2s), `SQL_DB_SKU` (Basic).
ACR and SQL server names are given a random suffix so they're globally unique;
pass `ACR_NAME` / `SQL_SERVER` to pin them.

This takes ~10 minutes (AKS creation dominates). It writes `.deploy.env`.

## Step 2 — Deploy the app

Export the four runtime secrets, then deploy:

```bash
export SQL_ADMIN_PASSWORD='<same-as-provisioning>'
export JWT_SECRET="$(openssl rand -base64 48)"
export SUPERADMIN_EMAIL='platform@yourorg.com'
export SUPERADMIN_PASSWORD='<strong-superadmin-password>'
./01-deploy.sh
```

The script builds the images in ACR (no local Docker needed), installs the
NGINX ingress controller, creates the `carelink-secrets` secret, applies the
manifests **with Azure SQL replacing the in-cluster SQL Server**, binds a
`*.nip.io` hostname to the ingress public IP, and waits for rollout.

On success it prints the app URL, e.g. `http://carelink.20.10.30.40.nip.io/`.

Sign in with the SuperAdmin credentials you set, then onboard hospitals through
the UI. The API migrates the schema and seeds the SuperAdmin automatically on
first start (`Database:MigrateOnStartup=true`); **no demo data is seeded in
production**.

---

## First login

- URL: the `http://...nip.io/` address printed by the deploy script.
- Account: `SUPERADMIN_EMAIL` / `SUPERADMIN_PASSWORD`.
- The SuperAdmin lands on the Hospitals page — provision your first real
  hospital (name, region, language, first admin). That admin gets a one-time
  temporary password to hand off.

## Custom domain + HTTPS (recommended before going public)

1. Point a DNS A record at the ingress IP (`kubectl get svc -n ingress-nginx`).
2. Re-run with `APP_HOST=carelink.yourdomain.com ./01-deploy.sh`, or
   `kubectl edit ingress carelink -n carelink`.
3. Add TLS with cert-manager (Let's Encrypt) and uncomment the `tls:` block in
   `deploy/k8s/ingress.yaml`.

---

## Wiring the Azure DevOps pipeline (continuous deploys)

`azure-pipelines.yml` at the repo root automates test → build/push → deploy.
After the one-time manual deploy above, connect it:

1. **Service connections**: `carelink-acr` (Docker Registry → your ACR),
   `carelink-aks` (Kubernetes → your AKS cluster).
2. **Environment** `carelink-production` with a required approval check.
3. **Variable group** `carelink-secrets` (all marked secret): `MSSQL_SA_PASSWORD`
   (your Azure SQL admin password), `JWT_SECRET`, `SUPERADMIN_EMAIL`,
   `SUPERADMIN_PASSWORD`, and optionally `APPINSIGHTS_CONNECTION_STRING`.
   > Note: the pipeline's secret step currently builds a connection string for
   > the in-cluster `sqlserver` host. For Azure SQL, set the variable-group
   > `MSSQL_SA_PASSWORD` and adjust that one `--from-literal` line to the Azure
   > SQL `Server=tcp:...` form (same string `01-deploy.sh` builds).
4. Update `registryHost` in `azure-pipelines.yml` to your ACR login server.

Pushes to `develop`/`main` then build & push images; `main` deploys to AKS
behind the approval gate.

---

## Monitoring

Application Insights is live automatically — the API and gateway export traces,
metrics, and logs (the connection string flows through `carelink-secrets`).
Find them in the Azure portal under the `carelink-ai` resource: **Application
Map**, **Live Metrics**, **Failures**, and **Logs** (KQL over the Log Analytics
workspace). Cloud role names are `carelink-api` and `carelink-gateway`.

---

## Cost (rough, East US, always-on)

| Resource | SKU | ~USD/mo |
|----------|-----|---------|
| AKS node | 1 × Standard_B2s (default) | ~30 |
| Azure SQL | Basic | ~5 |
| ACR | Basic | ~5 |
| Log Analytics + App Insights | pay-as-you-go | a few $ at low volume |
| Load balancer + egress | | a few $ |

Roughly **$45–55/month** at the free-trial-friendly defaults — **covered by the
$200 trial credit** for a demo. A production HA setup (`AKS_NODE_COUNT=2
REPLICAS=2`) is ~$75–100/mo. To pause spend between demo sessions, use AKS
**Stop/Start** (`az aks stop --name carelink-aks -g carelink-rg`).

## Teardown

Deletes **everything**, including the database:

```bash
az group delete --name carelink-rg --yes --no-wait
```
