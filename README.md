# Dotnet-Redis Starter App

Created a new project with `dotnet new console`.

# Redis

Create a container. You should only need to do this once.

The volume path has to be shared in Docker->Preferences->File Sharing.

Replace `/Users/joeyguerra/src/dotnet-redis-starter/myredis` with your local path.

```bash
docker create -v $(pwd)/myredis:/usr/local/etc/redis --name myredis -p 6379:6379 redis redis-server /usr/local/etc/redis
```

Start the container.

```bash
docker start myredis
```