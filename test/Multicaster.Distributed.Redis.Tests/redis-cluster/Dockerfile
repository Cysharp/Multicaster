FROM redis:8.10.1

LABEL org.opencontainers.image.title="Redis Cluster"
LABEL org.opencontainers.image.description="A lightweight Redis cluster setup for local development and testing. Based on the official Redis image and includes the create-cluster utility."
LABEL org.opencontainers.image.vendor="Ercan SORMAZ"
LABEL org.opencontainers.image.url="https://ercan.dev/projects/redis-cluster-docker-image"
LABEL org.opencontainers.image.source="https://github.com/ercansormaz/docker-redis-cluster"
LABEL org.opencontainers.image.documentation="https://github.com/ercansormaz/docker-redis-cluster/blob/main/README.md"
LABEL org.opencontainers.image.licenses="MIT"
LABEL org.opencontainers.image.version="8.10.1"
LABEL org.opencontainers.image.ref.name="ercansormaz/redis-cluster:8.10.1"

# Download create-cluster script from redis repository
ADD https://raw.githubusercontent.com/redis/redis/refs/tags/8.10.1/utils/create-cluster/create-cluster /usr/local/bin/create-cluster
RUN chmod +x /usr/local/bin/create-cluster

# Copy custom entrypoint script
COPY ./entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# Override create-cluster script settings
COPY ./config.sh /data/config.sh

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]

EXPOSE 30001 30002 30003 30004 30005 30006
