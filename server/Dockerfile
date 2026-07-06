FROM ubuntu:22.04

RUN apt-get update && apt-get install -y \
    libgles2 \
    libgl1 \
    libfontconfig1 \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY server/ .

RUN chmod +x game.x86_64

CMD ["./start.sh"]
