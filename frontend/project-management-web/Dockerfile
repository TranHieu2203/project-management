FROM node:20-alpine AS build
WORKDIR /app

COPY frontend/project-management-web/package.json frontend/project-management-web/package-lock.json ./
RUN npm ci

COPY frontend/project-management-web/ ./
RUN npm run build

FROM nginx:1.27-alpine
COPY frontend/project-management-web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/project-management-web/browser /usr/share/nginx/html

