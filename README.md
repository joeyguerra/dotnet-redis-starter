# Dotnet-Redis Starter App

Created a new project with `dotnet new console`.

# Redis

Create a container. You should only need to do this once.

The volume path has to be shared in Docker->Preferences->File Sharing.

```bash
# MacOS
docker create -v $(pwd)/redis-config:/usr/local/etc/redis --name myredis -p 6379:6379 redis redis-server /usr/local/etc/redis
```

```powershell
# Windows
docker create -v ${pwd}/redis-config:/usr/local/etc/redis --name myredis -p 6379:6379 redis redis-server /usr/local/etc/redis
```

Start the container.

```bash
docker start myredis
```


# Run Tests

Note: The Redis password is externalized by setting an environment variable when starting the service per the [12 Factor App](https://12factor.net/config).
```bash
cd DotnetRedisStarter.Tests
RedisPassword=<password that you shouldve set in redis.conf> dotnet test
```

Continously run the tests.

```bash
cd DotnetRedisStarter.Tests
RedisPassword=<password that you shouldve set in redis.conf> dotnet watch test
```

## Windows

```powershell
cd DotnetRedisStarter.Tests
$env:RedisPassword=<password that you shouldve set in redis.conf>
dotnet test
```

# Run App

Set a password in [redis.conf](https://github.com/joeyguerra/dotnet-redis-starter/blob/cab9806b419b9305c106e90017176c1f79309d6e/redis-config/redis-sample.conf#L790). This folder will get bind mounted to the Redis container per `docker create` command above.

```bash
dotnet run --project Reporter/Reporter.csproj RedisPassword=<password that you shouldve set in redis.conf>
```