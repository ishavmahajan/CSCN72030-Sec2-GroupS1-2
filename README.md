# KitchenOS SCADA

KitchenOS SCADA is an ASP.NET Core API (`.NET 10`) for the KitchenOS supervisory control system. The system design is in [docs/design.md](docs/design.md). Development runs inside Docker so the API, SDK, and file watcher are the same on every machine.

The API listens on [http://localhost:8080](http://localhost:8080). In Development it also serves an OpenAPI document at [http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json).

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose)
- Git
- `make` (already installed on macOS)

You do not need the .NET SDK on the host to run the API. The container uses the official .NET 10 SDK image.

## Install Docker

Docker Desktop is the supported way to run this project. Install it for your operating system, start it, and leave it running before you use `make` or `docker compose`.

### macOS

1. Download Docker Desktop from the [Mac install page](https://docs.docker.com/desktop/setup/install/mac-install/).
   - Apple silicon (M1, M2, M3, M4): choose **Mac with Apple chip**.
   - Intel Mac: choose **Mac with Intel chip**.
2. Open the downloaded `.dmg`.
3. Drag **Docker** into the **Applications** folder.
4. Open **Docker** from Applications.
5. Accept the service agreement and finish the welcome screens. macOS will ask for your password so Docker can install its privileged helper.
6. Wait until the whale icon in the menu bar is steady and Docker reports that the engine is running.

### Windows

1. Turn on WSL 2. In an elevated PowerShell window, run:

   ```powershell
   wsl --install
   ```

   Restart if Windows asks you to.
2. Download Docker Desktop from the [Windows install page](https://docs.docker.com/desktop/setup/install/windows-install/).
3. Run `Docker Desktop Installer.exe`.
4. On the configuration screen, leave **Use WSL 2 instead of Hyper-V** selected.
5. Finish the installer and restart if prompted.
6. Open **Docker Desktop** and wait until it says the engine is running.

### Linux

Install Docker Engine and the Compose plugin from Docker’s package repository. The [engine install docs](https://docs.docker.com/engine/install/) have steps for Ubuntu, Debian, Fedora, and other distributions.

On Ubuntu or Debian, the short version is:

```bash
sudo apt-get update
sudo apt-get install ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Debian users should replace `linux/ubuntu` with `linux/debian` in the two URLs above. See the [Debian install page](https://docs.docker.com/engine/install/debian/) for the exact commands.

Add your user to the `docker` group so you can run Docker without `sudo`, then log out and back in:

```bash
sudo usermod -aG docker $USER
```

### Confirm Docker is working

```bash
docker --version
docker compose version
docker run --rm hello-world
```

`hello-world` should print a short success message and then exit. If `docker` is not found, restart the terminal after installation. If the engine is not running, open Docker Desktop and wait until it is ready.

## Set up the project

Clone the repository and start the development stack:

```bash
git clone git@github.com:ishavmahajan/CSCN72030-Sec2-GroupS1-2.git
cd kitchenOS
make up
```

The first run builds the development image and restores NuGet packages, so it takes longer than later starts. When the API is ready, the logs show it listening on port 8080.

Check that it responds:

```bash
curl http://localhost:8080/health
```

Stop the stack with `Ctrl+C`, or from another terminal:

```bash
make down
```

Source files under `backend/kitchenOS_Scada.Api` are mounted into the container. `dotnet watch` rebuilds and restarts the API when you save a change.

## Common commands

| Command | What it does |
| --- | --- |
| `make up` | Start the API in the foreground |
| `make down` | Stop and remove containers |
| `make build` | Build the Docker image |
| `make rebuild` | Rebuild the image without cache |
| `make restart` | Restart running containers |
| `make logs` | Follow backend logs |
| `make ps` | Show container status |
| `make shell` | Open a shell in the backend container |
| `make restore` | Restore .NET dependencies inside the container |
| `make test` | Run `dotnet test` inside the container |
| `make clean` | Stop containers and remove volumes |

Run the stack in the background with:

```bash
docker compose up -d
```

## Project layout

```text
backend/kitchenOS_Scada.Api/   ASP.NET Core API
backend/kitchenOS_Scada.slnx   Solution file
docker-compose.yml             Development stack
Makefile                       Shortcuts for Docker Compose
```

The Compose file builds the `development` stage of `backend/kitchenOS_Scada.Api/Dockerfile`. That stage runs `dotnet watch`. The same Dockerfile has `build` and `production` stages for a published image.

## Run without Docker

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then:

```bash
cd backend/kitchenOS_Scada.Api
dotnet restore
dotnet run
```

That profile uses [http://localhost:5253](http://localhost:5253). The Docker workflow above is the one this repository is set up for.
