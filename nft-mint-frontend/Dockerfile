# syntax=docker/dockerfile:1.5

FROM node:20-alpine AS deps
WORKDIR /app
RUN apk add --no-cache python3 make g++ linux-headers libusb-dev eudev-dev
COPY package.json package-lock.json* ./
RUN npm ci

FROM node:20-alpine AS builder
WORKDIR /app
RUN apk add --no-cache python3 make g++ linux-headers libusb-dev eudev-dev
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production

COPY --from=builder /app/public ./public
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/package.json ./package.json
COPY --from=builder /app/package-lock.json ./package-lock.json
COPY --from=builder /app/next.config.* ./
COPY --from=deps /app/node_modules ./node_modules

RUN npm prune --omit=dev && \
    addgroup -S nextjs && adduser -S nextjs -G nextjs
USER nextjs

EXPOSE 3000
ENV PORT=3000

CMD ["npx", "next", "start"]

