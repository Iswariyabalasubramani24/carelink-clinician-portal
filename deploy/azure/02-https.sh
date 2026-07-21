#!/usr/bin/env bash
# Add trusted HTTPS to the deployed CareLink ingress using cert-manager +
# Let's Encrypt (HTTP-01 challenge). Run AFTER 01-deploy.sh, once the app is
# reachable over http://<host>/.
#
#   export CERT_EMAIL='you@example.com'   # ACME registration + expiry notices
#   ./02-https.sh                          # (or: bash ./02-https.sh)
#
# Idempotent - safe to re-run. Uses kubectl's current context (set by
# 01-deploy.sh via `az aks get-credentials`).
set -euo pipefail

# Read the host the deploy bound to the ingress (e.g. carelink.<ip>.nip.io).
APP_HOST="${APP_HOST:-$(kubectl get ingress carelink -n carelink -o jsonpath='{.spec.rules[0].host}')}"
[ -n "$APP_HOST" ] || { echo "ERROR: could not read ingress host - is the app deployed?"; exit 1; }

CERT_EMAIL="${CERT_EMAIL:-${SUPERADMIN_EMAIL:-}}"
if [ -z "$CERT_EMAIL" ]; then
  echo "ERROR: set CERT_EMAIL to an email address (used by Lets Encrypt for expiry notices)."
  exit 1
fi

echo ">> Host: $APP_HOST | ACME email: $CERT_EMAIL"

# ---- Install cert-manager (idempotent) -----------------------------------
echo ">> Installing cert-manager..."
helm repo add jetstack https://charts.jetstack.io >/dev/null 2>&1 || true
helm repo update >/dev/null
helm upgrade --install cert-manager jetstack/cert-manager \
  --namespace cert-manager --create-namespace \
  --set crds.enabled=true \
  --wait --timeout 5m

# ---- Let's Encrypt production issuer (HTTP-01 via ingress-nginx) ----------
kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: ${CERT_EMAIL}
    privateKeySecretRef:
      name: letsencrypt-prod-account-key
    solvers:
      - http01:
          ingress:
            class: nginx
EOF

# ---- Turn the existing ingress into a TLS ingress -------------------------
# The annotation tells cert-manager (ingress-shim) to obtain and renew a cert
# for the tls host into the carelink-tls secret; ingress-nginx then serves it
# and redirects HTTP -> HTTPS automatically.
kubectl annotate ingress carelink -n carelink \
  cert-manager.io/cluster-issuer=letsencrypt-prod --overwrite
kubectl patch ingress carelink -n carelink --type=json -p "[
  {\"op\":\"add\",\"path\":\"/spec/tls\",\"value\":[{\"hosts\":[\"${APP_HOST}\"],\"secretName\":\"carelink-tls\"}]}
]"

# ---- Wait for the certificate to be issued -------------------------------
echo ">> Requesting certificate from Let's Encrypt (usually under a minute)..."
if ! kubectl wait --for=condition=Ready certificate/carelink-tls -n carelink --timeout=240s; then
  echo "!! Certificate not Ready yet. Check progress with:"
  echo "     kubectl describe certificate carelink-tls -n carelink"
  echo "     kubectl get challenges -n carelink"
  exit 1
fi

echo ""
echo ">> HTTPS is live."
echo ">> Secure URL:  https://${APP_HOST}/"
echo ">> (http:// now redirects to https automatically.)"
