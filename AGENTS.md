Lern all projects for understand contexts.
Use mcp tools if need.

web:

- use pnpm, tailwindcss, shadcn, vite, ts, react.
- if only you modify any backend api endpoints, then use `pnpm generate-api` for generate client code (don't use it if you modify frontend code).
- for add shadcn components use `pnpm dlx shadcn@latest add <component_name>`.
- make user frendly and best practices UI/UX.
- use `react-router` intead `react-router-dom` (lib name changed)

backend:

- use current architecture.
- use microservice architecture.
- the system ups via Aspire AppHost project.
- divide domain logic on microservices.
- use best practices for coding.
- make classes small and understandable for people.
- write triple slash comments.
- make architecture code expandable and easy modifyable.
- use SAGA, retrying, resiliance if need for reduce errors count.
