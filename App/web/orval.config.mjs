import { defineConfig } from "orval";

export default defineConfig({
    songApi: {
        input: {
            target:
                process.env.WEBAPI_OPENAPI ??
                "http://localhost:5000/openapi/v1.json",
        },
        output: {
            mode: "tags",
            target: "./lib/generated/song-api/client.ts",
            schemas: "./lib/generated/song-api/models",
            client: "react-query",
            httpClient: "fetch",
            clean: true,
            formatter: "prettier",
        },
    },
    auth: {
        input: { target: "http://localhost:4000/openapi/v1.json" },
        output: {
            target: "./lib/generated/auth-api/client.ts",
            schemas: "./lib/generated/auth/models",
            client: "fetch",
            clean: true,
            formatter: "prettier",
        },
    },
});
