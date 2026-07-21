# Build context: frontend/
# docker build -t carelink-frontend -f Dockerfile .

FROM node:20-alpine AS build
WORKDIR /app

# Cypress is E2E-only tooling; skip its ~200MB binary download inside the image.
ENV CYPRESS_INSTALL_BINARY=0

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
EXPOSE 80
