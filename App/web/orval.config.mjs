import { defineConfig } from "orval";

export default defineConfig({
    webApi: {
        input: {
            target:
                process.env.WEBAPI_OPENAPI ??
                "http://localhost:5000/openapi/v1.json",
        },
        output: {
            mode: "single",
            target: "./lib/generated/web-api/client.ts",
            schemas: "./lib/generated/web-api/models",
            client: "react-query",
            httpClient: "axios",
            clean: true,
            formatter: "prettier",
            override: {
                mutator: {
                    path: "./lib/axios-instance.ts",
                    name: "customInstance",
                },
            },
        },
    },
    auth: {
        input: {
            target:
                process.env.AUTH_OPENAPI ??
                "http://localhost:4000/openapi/v1.json",
        },
        output: {
            target: "./lib/generated/auth-api/client.ts",
            schemas: "./lib/generated/auth/models",
            client: "fetch",
            clean: true,
            formatter: "prettier",
        },
    },
});
