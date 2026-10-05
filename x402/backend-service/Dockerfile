FROM node:20-alpine AS base

WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN apk add --no-cache curl
RUN npm ci --omit=dev

# Copy source
COPY src ./src
COPY bin ./bin
RUN mkdir -p /app/data

ENV NODE_ENV=production \
    X402_HOST=0.0.0.0 \
    X402_PORT=4000

EXPOSE 4000

CMD ["npm", "run", "start"]

