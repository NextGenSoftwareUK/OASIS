FROM node:23-alpine AS builder

WORKDIR /app

COPY package.json yarn.lock ./
RUN yarn install --frozen-lockfile --production

COPY . .

ENV NEXT_PUBLIC_API_URL=NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_SOLANA_ENVIRONMENT=NEXT_PUBLIC_SOLANA_ENVIRONMENT

RUN yarn build

FROM node:23-alpine AS runner

WORKDIR /app

COPY --from=builder /app ./

COPY src/utils/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 3000

ENTRYPOINT ["/entrypoint.sh"]
CMD ["yarn", "start"]
