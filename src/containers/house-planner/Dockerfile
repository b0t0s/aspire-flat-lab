FROM alpine:3.21 AS source

RUN apk add --no-cache git
ARG SOURCE_COMMIT=5c91280da6c5cc84c1a534ef80ddafb64a6b5baa
RUN git init /source \
    && git -C /source remote add origin https://github.com/egmalt/house-planner.git \
    && git -C /source fetch --depth 1 origin "$SOURCE_COMMIT" \
    && git -C /source checkout --detach FETCH_HEAD

FROM node:22-alpine AS build

WORKDIR /app
COPY --from=source /source/ ./
RUN npm ci --no-audit --no-fund \
    && npm run build \
    && rm -f dist/data/.htaccess dist/plans/.htaccess

FROM php:8.3-apache

ENV HOUSE_STORAGE=/var/lib/house-planner
RUN a2enmod rewrite headers \
    && a2dismod -f autoindex status \
    && mv "$PHP_INI_DIR/php.ini-production" "$PHP_INI_DIR/php.ini"

COPY --from=source /source/docker/apache/ports.conf /etc/apache2/ports.conf
COPY --from=source /source/docker/apache/site.conf /etc/apache2/sites-available/000-default.conf
COPY --from=source /source/docker/php/house-planner.ini "$PHP_INI_DIR/conf.d/zz-house-planner.ini"
COPY --from=source /source/docker/house-boot.php /usr/local/lib/house-planner/boot.php
COPY --from=source /source/docker/healthcheck.php /usr/local/lib/house-planner/healthcheck.php
COPY --from=source /source/docker/entrypoint.sh /usr/local/bin/house-entrypoint
COPY --from=build /app/dist/ /var/www/html/
RUN mkdir -p "$HOUSE_STORAGE" \
    && chown root:root /var/www/html \
    && chmod 755 /var/www/html \
    && chown -R www-data:www-data "$HOUSE_STORAGE" /var/www/html/data /var/www/html/plans

VOLUME /var/lib/house-planner
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["php", "/usr/local/lib/house-planner/healthcheck.php"]
ENTRYPOINT ["house-entrypoint"]
CMD ["apache2-foreground"]