#!/usr/bin/env bash
# Build, push, and deploy CareLink to the AKS cluster provisioned by
# 00-provision.sh. Uses `az acr build` so no local Docker is required.
#
# Prerequisites in your shell:
#   - Ran 00-provision.sh (creates deploy/azure/.deploy.env)
#   - az login done; kubectl + helm installed
#   - The following secrets exported (NEVER committed):
#       export SQL_ADMIN_PASSWORD='<the SQL admin password from provisioning>'
#       export JWT_SECRET="$(openssl rand -base64 48)"
#       export SUPERADMIN_EMAIL='platform@yourorg.com'
#       export SUPERADMIN_PASSWORD='<strong password>'
#
#   ./01-deploy.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
ENV_FILE="$SCRIPT_DIR/.deploy.env"

[ -f "$ENV_FILE" ] || { echo "ERROR: $ENV_FILE not found - run ./00-provision.sh first."; exit 1; }
# shellcheck disable=SC1090
source "$ENV_FILE"

: "${SQL_ADMIN_PASSWORD:?export SQL_ADMIN_PASSWORD before running}"
: "${JWT_SECRET:?export JWT_SECRET before running}"
: "${SUPERADMIN_EMAIL:?export SUPERADMIN_EMAIL before running}"
: "${SUPERADMIN_PASSWORD:?export SUPERADMIN_PASSWORD before running}"

IMAGE_TAG="${IMAGE_TAG:-$(date +%Y%m%d%H%M%S)}"

# ---- Build & push the three images directly in ACR -----------------------
echo ">> Building images in ACR (tag $IMAGE_TAG)..."
az acr build --registry "$ACR_NAME" --image "carelink-api:$IMAGE_TAG" \
  --file "$REPO_ROOT/CareLink.CleanArchitecture/Dockerfile" "$REPO_ROOT/CareLink.CleanArchitecture"
az acr build --registry "$ACR_NAME" --image "carelink-gateway:$IMAGE_TAG" \
  --file "$REPO_ROOT/api-gateway/Dockerfile" "$REPO_ROOT/api-gateway"
az acr build --registry "$ACR_NAME" --image "carelink-frontend:$IMAGE_TAG" \
  --file "$REPO_ROOT/frontend/Dockerfile" "$REPO_ROOT/frontend"

# ---- Cluster credentials -------------------------------------------------
az aks get-credentials --name "$AKS_NAME" --resource-group "$RESOURCE_GROUP" --overwrite-existing

# ---- Ingress controller (idempotent) -------------------------------------
echo ">> Ensuring ingress-nginx is installed..."
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx >/dev/null 2>&1 || true
helm repo update >/dev/null
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace \
  --set controller.service.externalTrafficPolicy=Local \
  --wait --timeout 5m

# ---- Namespace + runtime secret ------------------------------------------
kubectl apply -f "$REPO_ROOT/deploy/k8s/namespace.yaml"

SQL_CONN="Server=tcp:${SQL_FQDN},1433;Database=${SQL_DB};User Id=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;"
kubectl create secret generic carelink-secrets --namespace carelink \
  --from-literal=ConnectionStrings__DefaultConnection="$SQL_CONN" \
  --from-literal=Jwt__Secret="$JWT_SECRET" \
  --from-literal=SuperAdmin__Email="$SUPERADMIN_EMAIL" \
  --from-literal=SuperAdmin__Password="$SUPERADMIN_PASSWORD" \
  --from-literal=APPLICATIONINSIGHTS_CONNECTION_STRING="${APPINSIGHTS_CONNECTION_STRING:-}" \
  --dry-run=client -o yaml | kubectl apply -f -

# ---- Render manifests: Azure SQL replaces the in-cluster StatefulSet ------
# Work on a throwaway copy so the committed kustomization is never mutated,
# and drop sqlserver.yaml since Azure SQL is the database here.
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
cp -r "$REPO_ROOT/deploy/k8s/." "$STAGE/"
( cd "$STAGE"
  kustomize edit remove resource sqlserver.yaml
  kustomize edit set image \
    "carelink-api=${ACR_LOGIN_SERVER}/carelink-api:${IMAGE_TAG}" \
    "carelink-gateway=${ACR_LOGIN_SERVER}/carelink-gateway:${IMAGE_TAG}" \
    "carelink-frontend=${ACR_LOGIN_SERVER}/carelink-frontend:${IMAGE_TAG}"
)
kubectl apply -k "$STAGE"

# ---- Wait for the ingress public IP, then bind a hostname ----------------
echo ">> Waiting for the ingress load-balancer IP..."
for _ in $(seq 1 30); do
  LB_IP="$(kubectl get svc ingress-nginx-controller -n ingress-nginx \
    -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)"
  [ -n "$LB_IP" ] && break
  sleep 10
done
[ -n "${LB_IP:-}" ] || { echo "ERROR: ingress IP not assigned yet - check 'kubectl get svc -n ingress-nginx'."; exit 1; }

# nip.io gives instant wildcard DNS (<ip>.nip.io -> <ip>) so the app is
# reachable immediately without owning a domain. Swap for your real host later.
APP_HOST="${APP_HOST:-carelink.${LB_IP}.nip.io}"
kubectl patch ingress carelink -n carelink --type=json \
  -p "[{\"op\":\"replace\",\"path\":\"/spec/rules/0/host\",\"value\":\"${APP_HOST}\"}]"

# ---- Wait for rollouts ---------------------------------------------------
kubectl rollout status -n carelink deployment/api --timeout=240s
kubectl rollout status -n carelink deployment/gateway --timeout=120s
kubectl rollout status -n carelink deployment/frontend --timeout=120s

echo ""
echo ">> Deployment complete."
echo ">> App URL:  http://${APP_HOST}/"
echo ">> Sign in with the SuperAdmin account you configured, then onboard hospitals."
