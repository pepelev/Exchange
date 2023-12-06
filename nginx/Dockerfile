# syntax=docker/dockerfile:1

FROM nginx
COPY nginx.conf /etc/nginx/nginx.conf
RUN ["rm", "-rf", "/etc/nginx/conf.d"]
RUN ["rm", "-rf", "/docker-entrypoint.d"]
EXPOSE 80