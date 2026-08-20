# GitHub CI/CD

Workflow `.github/workflows/ci-cd.yml` validates only applications affected by a pull request.
Pushes to `staging` and `release` additionally publish immutable Docker images and update the
corresponding Docker Swarm services.

## Repository configuration

Create repository variables:

- `REGISTRY_NAMESPACE` — image namespace, for example `wband`.

Create repository secrets:

- `DOCKER_REGISTRY_URL` — registry host without `https://`, for example `registry.example.com`;
- `DOCKER_REGISTRY_USERNAME`;
- `DOCKER_REGISTRY_PASSWORD`.

Create GitHub Environments named `staging` and `production`. Add these variables to each
environment using the actual service names returned by `docker service ls`:

- `SWARM_WEBAPI_SERVICE`;
- `SWARM_MIXER_WORKER_SERVICE`;
- `SWARM_WEB_SERVICE`.

The `release` branch uses `production`; the `staging` branch uses `staging`.

## Swarm runner

Install a GitHub self-hosted runner on a Swarm manager and assign the labels `linux`, `docker`,
and `swarm-manager`. Its operating-system account must be able to run `docker service inspect`,
`docker service update`, `docker login`, and `docker logout` without interactive elevation.

The workflow runs pull-request code only on GitHub-hosted runners. The Swarm runner is used only
after successful validation and image publication for a push to a deployment branch.

## Branch protection

Protect `staging` and `release`, disallow direct pushes, and require the check
`Affected projects / result`. Production deployment is automatic and does not require an
Environment reviewer.

## Image names

- `${DOCKER_REGISTRY_URL}/${REGISTRY_NAMESPACE}/wband-webapi`;
- `${DOCKER_REGISTRY_URL}/${REGISTRY_NAMESPACE}/wband-mixer-worker`;
- `${DOCKER_REGISTRY_URL}/${REGISTRY_NAMESPACE}/wband-web`.

Every deployment uses the full Git commit SHA tag. The workflow also publishes a mutable
`staging` or `release` tag for operator convenience.
