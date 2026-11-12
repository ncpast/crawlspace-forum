FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:22-alpine
WORKDIR /app
ENV NODE_ENV=production HOST=0.0.0.0 PORT=4321
COPY package*json ./
RUN npm ci --omit=dev
COPY --from=build /app/dist ./dist
EXPOSE 4321
CMD ["node", "./dist/server/entry.mjs"]