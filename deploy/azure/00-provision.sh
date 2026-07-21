#!/usr/bin/env bash
# Provision all Azure infrastructure for CareLink: resource group, container
# registry, AKS cluster, Azure SQL, and Application Insights.
#
# Run once (idempotent-ish - re-running skips resources that already exist).
# You must be logged in first:  az login  &&  az account set --subscription <id>
#
#   export SQL_ADMIN_PASSWORD='<strong password>'   # required, min 8 chars, complex
#   ./00-provision.sh
#
# Non-secret outputs (registry host, App Insights connection string, SQL FQDN)
# are written to deploy/azure/.deploy.env for 01-deploy.sh to read. The SQL
# admin password is NOT written to disk - keep it in your shell / secret store.
set -euo pipefail

# ---- Configuration (override via environment) ----------------------------
LOCATION="${LOCATION:-eastus}"
RESOURCE_GROUP="${RESOURCE_GROUP:-carelink-rg}"
# ACR name must be globally unique, 5-50 chars, lowercase alphanumeric only.
ACR_NAME="${ACR_NAME:-carelinkacr$RANDOM}"
AKS_NAME="${AKS_NAME:-carelink-aks}"
# Default 1 node (2 vCPU) so a fresh Azure free trial - which typically has a
# 4 vCPU regional quota - can deploy without a quota-increase request. Bump to
# 2+ for a real production cluster.
AKS_NODE_COUNT="${AKS_NODE_COUNT:-1}"
AKS_NODE_SIZE="${AKS_NODE_SIZE:-Standard_B2s}"
LOG_WORKSPACE="${LOG_WORKSPACE:-carelink-logs}"
APPINSIGHTS_NAME="${APPINSIGHTS_NAME:-carelink-ai}"
# SQL server name must be globally unique, lowercase.
SQL_SERVER="${SQL_SERVER:-carelink-sql-$RANDOM}"
SQL_DB="${SQL_DB:-CareLinkCleanArch}"
SQL_ADMIN_USER="${SQL_ADMIN_USER:-carelinkadmin}"
SQL_DB_SKU="${SQL_DB_SKU:-Basic}" # Basic ~5 USD/mo; use S0/GP_S_Gen5_1 for more headroom

: "${SQL_ADMIN_PASSWORD:?Set SQL_ADMIN_PASSWORD (min 8 chars, upper+lower+digit+symbol) before running}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_ENV="$SCRIPT_DIR/.deploy.env"

echo ">> Subscription: $(az account show --query name -o tsv)"
echo ">> Region: $LOCATION | Resource group: $RESOURCE_GROUP"

# ---- Resource group ------------------------------------------------------
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
echo ">> Resource group ready."

# ---- Container registry --------------------------------------------------
if ! az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  az acr create --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" \
    --sku Basic --output none
fi
ACR_LOGIN_SERVER="$(az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" --query loginServer -o tsv)"
echo ">> ACR ready: $ACR_LOGIN_SERVER"

# ---- Observability: Log Analytics + workspace-based App Insights ---------
az monitor log-analytics workspace create \
  --resource-group "$RESOURCE_GROUP" --workspace-name "$LOG_WORKSPACE" \
  --location "$LOCATION" --output none
WORKSPACE_ID="$(az monitor log-analytics workspace show \
  --resource-group "$RESOURCE_GROUP" --workspace-name "$LOG_WORKSPACE" --query id -o tsv)"

az extension add --name application-insights --only-show-errors 2>/dev/null || true
if ! az monitor app-insights component show --app "$APPINSIGHTS_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  az monitor app-insights component create \
    --app "$APPINSIGHTS_NAME" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" \
    --workspace "$WORKSPACE_ID" --output none
fi
APPINSIGHTS_CONNECTION_STRING="$(az monitor app-insights component show \
  --app "$APPINSIGHTS_NAME" --resource-group "$RESOURCE_GROUP" --query connectionString -o tsv)"
echo ">> Application Insights ready."

# ---- AKS cluster (ACR attached, monitoring addon) ------------------------
if ! az aks show --name "$AKS_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  az aks create \
    --name "$AKS_NAME" --resource-group "$RESOURCE_GROUP" \
    --node-count "$AKS_NODE_COUNT" --node-vm-size "$AKS_NODE_SIZE" \
    --attach-acr "$ACR_NAME" \
    --enable-addons monitoring --workspace-resource-id "$WORKSPACE_ID" \
    --generate-ssh-keys --output none
fi
echo ">> AKS ready: $AKS_NAME"

# ---- Azure SQL server + database -----------------------------------------
if ! az sql server show --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  az sql server create \
    --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" \
    --admin-user "$SQL_ADMIN_USER" --admin-password "$SQL_ADMIN_PASSWORD" --output none
fi
# Allow other Azure services (i.e. the AKS pods) to reach the SQL server.
az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --output none
if ! az sql db show --name "$SQL_DB" --server "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  az sql db create \
    --name "$SQL_DB" --server "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" \
    --service-objective "$SQL_DB_SKU" --backup-storage-redundancy Local --output none
fi
SQL_FQDN="$(az sql server show --name "$SQL_SERVER" --resource-group "$RESOURCE_GROUP" --query fullyQualifiedDomainName -o tsv)"
echo ">> Azure SQL ready: $SQL_FQDN / $SQL_DB"

# ---- Persist non-secret outputs for the deploy step ----------------------
cat > "$OUT_ENV" <<EOF
# Generated by 00-provision.sh - non-secret deployment outputs.
# Consumed by 01-deploy.sh. Safe to keep locally; gitignored.
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
ACR_NAME=$ACR_NAME
ACR_LOGIN_SERVER=$ACR_LOGIN_SERVER
AKS_NAME=$AKS_NAME
SQL_SERVER=$SQL_SERVER
SQL_FQDN=$SQL_FQDN
SQL_DB=$SQL_DB
SQL_ADMIN_USER=$SQL_ADMIN_USER
APPINSIGHTS_CONNECTION_STRING=$APPINSIGHTS_CONNECTION_STRING
EOF

echo ""
echo ">> Provisioning complete. Outputs written to $OUT_ENV"
echo ">> Next: export the runtime secrets and run ./01-deploy.sh (see README.md)."
